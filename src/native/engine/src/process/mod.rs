use ffi_ext::win32::*;
use std::*;

mod exited;

#[repr(C)]
#[derive(Debug)]
pub struct Basic {
    pub id: u32
}

#[repr(C)]
#[derive(Debug)]
pub struct Extended {
    pub handle: wintypes::HANDLE
}

impl Drop for Extended {
    fn drop(&mut self) {
        unsafe { handleapi::CloseHandle(self.handle) };
    }
}

#[repr(C)]
#[derive(Debug)]
pub struct Identification {
    pub path: ffi_ext::String,
    pub cmd_line: ffi_ext::String,
}

#[repr(C)]
#[derive(Debug)]
pub struct FileInfo {
    pub name: ffi_ext::String,
    pub description: ffi_ext::String
}

#[no_mangle]
pub unsafe fn process_extended(basic: &Basic) -> Extended {
    let handle = processthreadsapi::OpenProcess(
        winnt::PROCESS_VM_READ | winnt::PROCESS_QUERY_INFORMATION | winnt::SYNCHRONIZE,
        0, basic.id);
    Extended { handle }
}

#[no_mangle]
pub unsafe fn process_identification(extended: &Extended) -> Identification {
    let handle = extended.handle;
    let mut info: ntpsapi::PROCESS_BASIC_INFORMATION = mem::zeroed();
    let mut info_len = 0u32;
    ntpsapi::NtQueryInformationProcess(
        handle, 0,
        &mut info as *mut _ as *mut ctypes::c_void,
        mem::size_of::<ntpsapi::PROCESS_BASIC_INFORMATION>() as u32,
        &mut info_len as &mut u32); // TODO check all these values!

    let peb = ffi_ext::read_struct!(handle, info.PebBaseAddress, ntpebteb::PEB);
    let user_params = ffi_ext::read_struct!(handle, peb.ProcessParameters, ntrtl::RTL_USER_PROCESS_PARAMETERS);

    let path = ffi_ext::read_unicode_string!(handle, user_params.ImagePathName);
    let cmd_line = ffi_ext::read_unicode_string!(handle, user_params.CommandLine);
    Identification { path, cmd_line }
}

#[no_mangle]
pub fn process_file_info(identification: &Identification) -> FileInfo {
    let file_map = pelite::FileMap::open(&path::Path::new(&identification.path.to_os_string()))
        .expect("cannot open the file specified");
    let image = pelite::PeFile::from_bytes(file_map.as_ref())
        .expect("file is not a PE image");
    let resources = image.resources().expect("resources not found");
    let version_info = resources.version_info().expect("version info not found");
    let default_lang = version_info.translation()[0];

    iter::once(default_lang).chain(FALLBACK_LANGS.iter().map(|x| lang(x)))
        .find_map(|lang| file_info_from_version_info(version_info, lang))
        .expect("Name and description not found in any language")
}

pub static FALLBACK_LANGS: &[&str] = &[
    "040904B0", // US English + CP_UNICODE
    "040904E4", // US English + CP_USASCII
    "04090000"  // US English + unknown codepage
];

pub fn file_info_from_version_info(
    version_info: pelite::resources::version_info::VersionInfo,
    lang: pelite::resources::version_info::Language)
    -> Option<FileInfo> {
    let name = version_info.value(lang, "ProductName").map(|x| ffi_ext::String::from(x));
    let desc = version_info.value(lang, "FileDescription").map(|x| ffi_ext::String::from(x));
    name.zip(desc).map(|(name, description)| FileInfo { name, description })
}

pub fn lang(s: &'static str) -> pelite::resources::version_info::Language {
    let buf = ffi_ext::String::from_str(s).into_vec();
    pelite::resources::version_info::Language::parse(&buf[..]).expect("Cannot parse language")
}

pub unsafe fn path_fast(id: u32) -> ffi_ext::String {
    let handle = processthreadsapi::OpenProcess(
        winnt::PROCESS_VM_READ | winnt::PROCESS_QUERY_LIMITED_INFORMATION,
        0, id);
    let mut len = 1024u32; // TODO macro this pattern
    let buf = loop {
        let mut buf = ffi_ext::buffer!(len);
        let res = winbase::QueryFullProcessImageNameW(handle, 0, buf.as_mut_ptr(), &mut len);
        if res != 0 { break ffi_ext::buffer_to_string!(&buf[..len as usize]) }
        len *= 2;
    };
    handleapi::CloseHandle(handle);
    buf
}
