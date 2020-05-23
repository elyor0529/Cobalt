extern crate winapi;
extern crate ntapi;
extern crate chrono;

use chrono::prelude::*;
use winapi::um::*;
use winapi::shared::*;
use ntapi::*;
use std::*;

macro_rules! read_unicode_string {
    ($p: expr, $str: expr) => {{
        let mut buf_len = $str.Length as usize / 2;
        let mut buf = vec![0u16; buf_len];
        let res = memoryapi::ReadProcessMemory($p,
            $str.Buffer as *const ffi::c_void,
            buf.as_mut_ptr() as *mut _ as *mut ffi::c_void,
            buf_len * 2,
            &mut buf_len
        );
        if res == 0 { panic!(std::io::Error::last_os_error().to_string()) };
        String::from_utf16_lossy(&buf)
    }};
}

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
    win_event_hook: windef::HWINEVENTHOOK,
    event: minwindef::DWORD,
    hwnd: windef::HWND,
    id_object: winnt::LONG,
    id_child: winnt::LONG,
    id_event_thread: minwindef::DWORD,
    dwms_event_time: minwindef::DWORD) {
    let timestamp = dwms_event_to_instant(dwms_event_time);

    let mut pid = 0;
    let tid = winuser::GetWindowThreadProcessId(hwnd, &mut pid);

    let proc = processthreadsapi::OpenProcess(winnt::PROCESS_VM_READ | winnt::PROCESS_QUERY_INFORMATION, 0, pid);
    if proc.as_ref().is_none() { panic!(std::io::Error::last_os_error().to_string()) };

    let mut path_sz = 1024u32;
    let path = loop {
        let mut buf = vec![0u16; path_sz as usize];
        let res = winbase::QueryFullProcessImageNameW(proc, 0, buf.as_mut_ptr(), &mut path_sz);
        if res == 0 { panic!(std::io::Error::last_os_error().to_string()) };
        if buf[path_sz as usize] == 0u16 { break String::from_utf16_lossy(&buf[..path_sz as usize]); }
        path_sz *= 2;
    };

    let title = {
        let title_len = winuser::GetWindowTextLengthW(hwnd);
        let mut buf = vec![0u16; title_len as usize];
        let read = winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), title_len + 1);
        if read != title_len { panic!(std::io::Error::last_os_error().to_string()) };
        String::from_utf16_lossy(&buf[..title_len as usize])
    };

    let mut info: ntpsapi::PROCESS_BASIC_INFORMATION = mem::zeroed();
    let mut info_len = 0u32;
    let res = ntpsapi::NtQueryInformationProcess(proc, 0,
        &mut info as *mut _ as *mut ffi::c_void,
        mem::size_of::<ntpsapi::PROCESS_BASIC_INFORMATION>() as u32,
        &mut info_len as &mut u32);
    if res != 0 { panic!(std::io::Error::last_os_error().to_string()) };

    let mut peb: ntpebteb::PEB = mem::zeroed();
    let res = memoryapi::ReadProcessMemory(proc,
        info.PebBaseAddress as *const ffi::c_void,
        &mut peb as *mut _ as *mut ffi::c_void,
        mem::size_of::<ntpebteb::PEB>(),
        ptr::null_mut()
    );
    if res == 0 { panic!(std::io::Error::last_os_error().to_string()) };
    
    let mut user_params: ntrtl::RTL_USER_PROCESS_PARAMETERS = mem::zeroed();
    let res = memoryapi::ReadProcessMemory(proc,
        peb.ProcessParameters as *const ffi::c_void,
        &mut user_params as *mut _ as *mut ffi::c_void,
        mem::size_of::<ntrtl::RTL_USER_PROCESS_PARAMETERS>(),
        ptr::null_mut()
    );
    if res == 0 { panic!(std::io::Error::last_os_error().to_string()) };

    let path2 = read_unicode_string!(proc, user_params.ImagePathName);
    let cmd_line = read_unicode_string!(proc, user_params.CommandLine);

    dbg!(timestamp);
    dbg!(path);
    dbg!(title);

    dbg!(path2);
    dbg!(cmd_line);

    handleapi::CloseHandle(proc);
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    unsafe {
        winuser::SetWinEventHook(
            winuser::EVENT_SYSTEM_FOREGROUND,
            winuser::EVENT_SYSTEM_FOREGROUND,
            ptr::null_mut(),
            Some(handler),
            0,
            0,
            0);
        event_loop();
        Ok(())
    }
}
