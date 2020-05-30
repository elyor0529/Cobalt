use crate::util::*;
use crate::info::*;
use winapi::um::*;
use winapi::shared::*;
use std::*;

pub struct ForegroundWindowWatcher {
    pub hook: windef::HWINEVENTHOOK,
    pub sub: Subscription<ForegroundWindowSwitch>
}

#[repr(C)]
pub struct BasicWindowInfo {
    pub id: windef::HWND,
    pub title: FfiString
}

#[repr(C)]
pub struct ForegroundWindowSwitch {
    pub win: BasicWindowInfo,
    pub filetime_ticks: i64
}

pub static mut FOREGROUND_WINDOW_WATCHER_INSTANCE: Option<ForegroundWindowWatcher> = None;

#[no_mangle]
pub unsafe fn add() -> FfiResult<u32> {
    FfiResult::Ok(1337)
}

#[no_mangle]
pub unsafe fn foreground_window_watcher_begin(sub: Subscription<ForegroundWindowSwitch>) {
    let hook = winuser::SetWinEventHook(
        winuser::EVENT_SYSTEM_FOREGROUND,
        winuser::EVENT_SYSTEM_FOREGROUND,
        ptr::null_mut(),
        Some(foreground_window_watcher_handler),
        0, 0,
        winuser::WINEVENT_OUTOFCONTEXT);
    FOREGROUND_WINDOW_WATCHER_INSTANCE = Some(ForegroundWindowWatcher { hook, sub });
}

#[no_mangle]
pub unsafe fn foreground_window_watcher_end() {
    let watcher = FOREGROUND_WINDOW_WATCHER_INSTANCE.as_ref().unwrap();
    winuser::UnhookWinEvent(watcher.hook); // TODO check errs
    FOREGROUND_WINDOW_WATCHER_INSTANCE = None;
}

unsafe extern "system" fn foreground_window_watcher_handler(
    _win_event_hook: windef::HWINEVENTHOOK,
    _event: minwindef::DWORD,
    hwnd: windef::HWND,
    _id_object: winnt::LONG,
    _id_child: winnt::LONG,
    _id_event_thread: minwindef::DWORD,
    dwms_event_time: minwindef::DWORD) {
    if winuser::IsWindow(hwnd) == 0 || winuser::IsWindowVisible(hwnd) == 0 { return; }

    let watcher = FOREGROUND_WINDOW_WATCHER_INSTANCE.as_ref().unwrap();
    let title = window::window_title(hwnd);
    let ticks = to_filetime_ticks(dwms_event_time);
    let win = BasicWindowInfo { id: hwnd, title  };
    let fg_switch = ForegroundWindowSwitch { win, filetime_ticks: ticks };
    (watcher.sub.on_next)(&fg_switch);
}

#[no_mangle]
pub unsafe fn to_filetime_ticks(ticks: minwindef::DWORD) -> i64 {
    let mut ft: minwindef::FILETIME = mem::zeroed();
    sysinfoapi::GetSystemTimePreciseAsFileTime(&mut ft);
    let millis_diff = ticks as i64 - sysinfoapi::GetTickCount64() as i64;
    let ticks = *(&mut ft as *mut _ as *mut i64);
    ticks + millis_diff * 10_000
}
