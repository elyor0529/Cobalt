use proc_macros::*;
use ffi_ext::{completed, next};
use ffi_ext::win32::*;
use watchers::*;
use std::*;
use crate::*;

#[repr(C)]
#[derive(Debug)]
pub struct Basic {
    pub hwnd: windef::HWND,
    pub title: ffi_ext::String
}

/*#[repr(C)]
#[derive(Debug)]
pub struct Extended {
    uwp_aumid: ffi_ext::Nullable<ffi_ext::String>
}*/

#[repr(C)]
#[derive(Debug)]
pub struct ForegroundWindowSwitch {
    pub win: window::Basic,
    pub filetime_ticks: i64
}

#[derive(Debug)]
pub struct ForegroundWindowWatcher<'a> {
    pub sub: &'a ffi_ext::Subscription<ForegroundWindowSwitch>,
    pub hook: windef::HWINEVENTHOOK
}

#[watcher_impl]
impl<'a> SingletonWatcher<'a, ForegroundWindowSwitch> for ForegroundWindowWatcher<'a> {

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
        completed!(self.sub);
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
    let ForegroundWindowWatcher { sub, .. } = instance!(ForegroundWindowWatcher);
    if winuser::IsWindow(hwnd) == 0 || winuser::IsWindowVisible(hwnd) == 0 { return; }

    let title = window::title(hwnd);
    let ticks = ticks_to_filetime(dwms_event_time);
    let win = window::Basic { hwnd, title };
    let fg_switch = ForegroundWindowSwitch { win, filetime_ticks: ticks };
    next!(sub, &fg_switch);
}

#[repr(C)]
#[derive(Debug)]
pub struct WindowClosed<'a> {
    pub hwnd: windef::HWND,
    pub hook: windef::HWINEVENTHOOK,
    pub sub: &'a ffi_ext::Subscription<()>,
}

#[watcher_impl]
impl<'a> TransientWatcher<'a, ffi_ext::Ptr<windef::HWND>, ()> for WindowClosed<'a> {
    fn begin(win: ffi_ext::Ptr<windef::HWND>, sub: &'a ffi_ext::Subscription<()>) -> Self {

        // TODO init hashmap

        let mut pid = 0;
        let tid = unsafe { winuser::GetWindowThreadProcessId(win.0, &mut pid) };
        let hook = unsafe { winuser::SetWinEventHook(
            winuser::EVENT_OBJECT_DESTROY,
            winuser::EVENT_OBJECT_DESTROY,
            ptr::null_mut(),
            Some(window_closed_handler),
            pid, tid,
            winuser::WINEVENT_OUTOFCONTEXT) };
        WindowClosed { hwnd: win.0, sub, hook }
    }

    fn end(self) {
        completed!(self.sub);
        unsafe { winuser::UnhookWinEvent(self.hook); }
    }
}

unsafe extern "system" fn window_closed_handler(
    _win_event_hook: windef::HWINEVENTHOOK,
    _event: minwindef::DWORD,
    hwnd: windef::HWND,
    id_object: winnt::LONG,
    id_child: winnt::LONG,
    _id_event_thread: minwindef::DWORD,
    _dwms_event_time: minwindef::DWORD) {
    if id_object != winuser::OBJID_WINDOW || id_child != 0 { return; }

}

/*
lazy_static! {
    static ref WINDOW_CLOSED_GLOBALS: sync::Mutex<HashMap<Ptr<windef::HWND>, WindowClosed>> = {
        sync::Mutex::new(HashMap::new())
    };
}
*/


#[no_mangle]
pub unsafe fn title(hwnd: windef::HWND) -> ffi_ext::String {
    let len = winuser::GetWindowTextLengthW(hwnd);
    let mut buf = vec![0u16; len as usize+1];
    winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), len+1);
    buf.set_len(len as usize); // Do not include the u16 null byte at the end
    ffi_ext::String::from_vec(buf)
}
