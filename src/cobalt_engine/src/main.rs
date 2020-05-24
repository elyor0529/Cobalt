extern crate winapi;
extern crate ntapi;
extern crate chrono;

#[macro_use]
mod utils;

use chrono::prelude::*;
use winapi::um::*;
use winapi::shared::*;
use ntapi::*;
use std::*;

unsafe fn event_loop(){
    loop {
        let mut msg: winuser::MSG = std::mem::zeroed();
        if 0 == winuser::GetMessageW(&mut msg, ptr::null_mut(), 0,0) { break }
        winuser::TranslateMessage(&mut msg);
        winuser::DispatchMessageW(&mut msg);
    }
}

unsafe fn dwms_event_to_instant(dwms_ticks: minwindef::DWORD) -> DateTime<Utc> {
    let sys_ticks = sysinfoapi::GetTickCount64();
    Utc::now() + chrono::Duration::milliseconds(dwms_ticks as i64 - sys_ticks as i64)
}

unsafe extern "system" fn handler(
    _win_event_hook: windef::HWINEVENTHOOK,
    _event: minwindef::DWORD,
    hwnd: windef::HWND,
    _id_object: winnt::LONG,
    _id_child: winnt::LONG,
    _id_event_thread: minwindef::DWORD,
    dwms_event_time: minwindef::DWORD) {
    let timestamp = dwms_event_to_instant(dwms_event_time);

    let mut pid = 0;
    let tid = winuser::GetWindowThreadProcessId(hwnd, &mut pid);

    let proc = processthreadsapi::OpenProcess(winnt::PROCESS_VM_READ | winnt::PROCESS_QUERY_INFORMATION, 0, pid);
    if proc.as_ref().is_none() { panic_win32!() };

    let title = {
        let title_len = winuser::GetWindowTextLengthW(hwnd);
        let mut buf = vec![0u16; title_len as usize];
        let read = winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), title_len + 1);
        if read != title_len { panic_win32!() };
        String::from_utf16_lossy(&buf[..title_len as usize])
    };

    let mut info: ntpsapi::PROCESS_BASIC_INFORMATION = mem::zeroed();
    let mut info_len = 0u32;
    let res = ntpsapi::NtQueryInformationProcess(proc, 0,
        &mut info as *mut _ as *mut ffi::c_void,
        mem::size_of::<ntpsapi::PROCESS_BASIC_INFORMATION>() as u32,
        &mut info_len as &mut u32);
    if res != 0 { panic_win32!() };

    let peb = read_struct!(proc, info.PebBaseAddress, ntpebteb::PEB);
    let user_params = read_struct!(proc, peb.ProcessParameters, ntrtl::RTL_USER_PROCESS_PARAMETERS);

    let path = read_unicode_string!(proc, user_params.ImagePathName);
    let cmd_line = read_unicode_string!(proc, user_params.CommandLine);

    dbg!(timestamp);
    dbg!(title);

    dbg!(path);
    dbg!(cmd_line);

    handleapi::CloseHandle(proc);
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    unsafe {
        let hook = winuser::SetWinEventHook(
            winuser::EVENT_SYSTEM_FOREGROUND,
            winuser::EVENT_SYSTEM_FOREGROUND,
            ptr::null_mut(),
            Some(handler),
            0,
            0,
            0);
        if hook.as_ref().is_none() { panic_win32!() }
        event_loop();
        Ok(())
    }
}
