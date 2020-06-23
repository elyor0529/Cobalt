use proc_macros::*;
use ffi_ext::win32::*;
use std::*;
use crate::*;

pub trait SingleInstanceWatcher<'a, T> {
    fn begin(sub: &'a ffi_ext::Subscription<T>) -> Self;
    fn end(self); 
    fn subscription(&self) -> &ffi_ext::Subscription<T>;
}

pub trait Watcher<'a, T> {
}

#[repr(C)]
pub struct ForegroundWindowSwitch {
    pub win: window::Basic,
    pub filetime_ticks: i64
}

pub struct ForegroundWindowWatcher<'a> {
    pub hook: windef::HWINEVENTHOOK,
    pub sub: &'a ffi_ext::Subscription<ForegroundWindowSwitch>
}

#[watcher_impl]
impl<'a> SingleInstanceWatcher<'a, ForegroundWindowSwitch> for ForegroundWindowWatcher<'a> {

    #[inline(always)]
    fn subscription(&self) -> &ffi_ext::Subscription<ForegroundWindowSwitch> { self.sub }

    fn begin(sub: &'a ffi_ext::Subscription<ForegroundWindowSwitch>) -> Self {
        let hook = unsafe {
            winuser::SetWinEventHook(
                winuser::EVENT_SYSTEM_FOREGROUND,
                winuser::EVENT_SYSTEM_FOREGROUND,
                ptr::null_mut(),
                Some(foreground_window_watcher_handler),
                0, 0,
                winuser::WINEVENT_OUTOFCONTEXT) };
        ForegroundWindowWatcher { hook, sub }
    }

    fn end(self) {
        unsafe { winuser::UnhookWinEvent(self.hook); }
    }
}

unsafe extern "system" fn foreground_window_watcher_handler(
    _win_event_hook: windef::HWINEVENTHOOK,
    _event: minwindef::DWORD,
    hwnd: windef::HWND,
    _id_object: winnt::LONG,
    _id_child: winnt::LONG,
    _id_event_thread: minwindef::DWORD,
    dwms_event_time: minwindef::DWORD) {
    let sub  = instance!(ForegroundWindowWatcher).subscription();
    if winuser::IsWindow(hwnd) == 0 || winuser::IsWindowVisible(hwnd) == 0 { return; }

    /*let title = window::title(hwnd);
    let ticks = to_filetime_ticks(dwms_event_time);
    let win = window::Basic { id: hwnd, title  };
    let fg_switch = ForegroundWindowSwitch { win, filetime_ticks: ticks };
    ffi_ext::next!(sub, &fg_switch);*/
}

#[no_mangle] // TODO move this to win32
pub unsafe fn to_filetime_ticks(ticks: minwindef::DWORD) -> i64 {
    let mut ft: minwindef::FILETIME = mem::zeroed();
    sysinfoapi::GetSystemTimePreciseAsFileTime(&mut ft);
    let millis_diff = ticks as i64 - sysinfoapi::GetTickCount64() as i64;
    let ticks = *(&mut ft as *mut _ as *mut i64);
    ticks + millis_diff * 10_000
}


