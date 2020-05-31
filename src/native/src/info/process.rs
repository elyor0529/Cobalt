use crate::util::*;
use winapi::um::*;
use winapi::shared::*;
use winapi::*;
use ntapi::*;
use std::*;
use std::os::windows::prelude::*;

#[repr(C)]
pub struct Process {
    id: u32,
    handle: *mut ctypes::c_void,
    path: FfiString,
    cmd_line: FfiString // TODO change all FfiString to OsString
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

    Process { id, handle, path, cmd_line }
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
