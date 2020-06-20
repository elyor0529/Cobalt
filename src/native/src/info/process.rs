use crate::util::*;
use winapi::um::*;
use winapi::shared::*;
use winapi::*;
use ntapi::*;
use std::*;
use std::os::windows::prelude::*;
use pelite::*;

#[repr(C)]
pub struct Process {
    id: u32,
    handle: *mut ctypes::c_void,
    path: FfiString,
    cmd_line: FfiString, // TODO change all FfiString to OsString
    name: FfiString,
    description: FfiString
}

#[no_mangle]
pub unsafe fn process_id_for_window(win: windef::HWND) -> u32 {
    let mut id = 0;
    let _ = winuser::GetWindowThreadProcessId(win, &mut id);
    id
}

#[no_mangle]
pub unsafe fn process_information(id: u32) -> Process {
    let handle = processthreadsapi::OpenProcess(winnt::PROCESS_VM_READ | winnt::PROCESS_QUERY_INFORMATION | winnt::SYNCHRONIZE, 0, id);

    let mut info: ntpsapi::PROCESS_BASIC_INFORMATION = mem::zeroed();
    let mut info_len = 0u32;
    let res = ntpsapi::NtQueryInformationProcess(handle, 0,
        &mut info as *mut _ as *mut ctypes::c_void,
        mem::size_of::<ntpsapi::PROCESS_BASIC_INFORMATION>() as u32,
        &mut info_len as &mut u32); // TODO check all these values!

    let peb = read_struct!(handle, info.PebBaseAddress, ntpebteb::PEB);
    let user_params = read_struct!(handle, peb.ProcessParameters, ntrtl::RTL_USER_PROCESS_PARAMETERS);

    let path = read_unicode_string!(handle, user_params.ImagePathName);
    let cmd_line = read_unicode_string!(handle, user_params.CommandLine);

    let file_map = FileMap::open(&path::Path::new(&ffi::OsString::from_wide(&path[..])))
        .expect("cannot open the file specified");
    let image = PeFile::from_bytes(file_map.as_ref())
	.expect("file is not a PE image");
    let resources = image.resources().expect("resources not found");
    let version_info = resources.version_info().expect("version info not found");
    let default_lang = version_info.translation()[0];

    let (name, description) = iter::once(default_lang).chain(FALLBACK_LANGS.iter().map(|x| lang(x)))
        .find_map(|lang| get_file_details(version_info, lang))
        .expect("Name and description not found in any language");
    Process { id, handle, path, cmd_line, name, description }
}

pub static FALLBACK_LANGS: &[&str] = &[
    "040904B0", // US English + CP_UNICODE
    "040904E4", // US English + CP_USASCII
    "04090000"  // US English + unknown codepage
];

fn get_file_details(version_info: resources::version_info::VersionInfo, lang: resources::version_info::Language) -> Option<(FfiString, FfiString)> {
    let name = version_info.value(lang, "ProductName").map(to_ffi_string);
    let desc = version_info.value(lang, "FileDescription").map(to_ffi_string);
    name.zip(desc)
}

fn to_ffi_string(s: String) -> FfiString {
    ffi::OsStr::new(s.as_str()).encode_wide().chain(iter::once(0)).collect()
}

fn lang(s: &'static str) -> resources::version_info::Language {
    let buf = ffi::OsStr::new(s).encode_wide().chain(iter::once(0)).collect::<Vec<u16>>();
    resources::version_info::Language::parse(&buf[..]).expect("Cannot parse language")
}

#[no_mangle]
pub unsafe fn process_information_drop(mut proc: mem::ManuallyDrop<Process>) {
    handleapi::CloseHandle(proc.handle);
    mem::ManuallyDrop::drop(&mut proc)
}

pub unsafe fn process_path_fast(id: u32) -> ffi::OsString {
    let handle = processthreadsapi::OpenProcess(winnt::PROCESS_VM_READ | winnt::PROCESS_QUERY_LIMITED_INFORMATION, 0, id);
    let mut len = 1024u32;
    let buf = loop {
        let mut buf = vec![0u16; len as usize];
        let res = winbase::QueryFullProcessImageNameW(handle, 0, buf.as_mut_ptr(), &mut len);
        if res != 0 { break ffi::OsString::from_wide(&buf[..len as usize]) }
        len *= 2;
    };
    handleapi::CloseHandle(handle);
    buf
}

#[repr(C)]
pub struct ProcessExit {
    sub: Subscription<()>,
    wait: *mut ctypes::c_void
}

#[no_mangle]
pub unsafe extern "system" fn process_exit_handler(dat: *mut ctypes::c_void, _: u8) {
    let process_exit = Box::from_raw(dat as *mut ProcessExit);
    (process_exit.sub.on_next)(&());
    (process_exit.sub.on_complete)();
    winbase::UnregisterWait(process_exit.wait);
}

#[no_mangle]
pub unsafe fn process_exit_begin(sub: Subscription<()>, proc: *mut ctypes::c_void) -> *mut ctypes::c_void {
    let process_exit = Box::leak(Box::new(ProcessExit { sub, wait: mem::zeroed() }));
    winbase::RegisterWaitForSingleObject(&mut process_exit.wait,
        proc, Some(process_exit_handler),
        process_exit as *mut _ as *mut ctypes::c_void,
        winbase::INFINITE, winnt::WT_EXECUTEONLYONCE);
    process_exit.wait
}
