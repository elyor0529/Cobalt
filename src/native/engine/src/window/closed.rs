use proc_macros::*;
use ffi_ext::{completed, next};
use ffi_ext::win32::*;
use watchers::*;
use std::*;
use crate::*;
use crate::window::pid_tid;

#[repr(C)]
pub struct WindowClosed<'a> {
    pub hwnd: ffi_ext::Ptr<wintypes::HWND>,
    pub hook: ffi_ext::Ptr<wintypes::HWINEVENTHOOK>,
    pub sub: &'a ffi_ext::Subscription<()>,
}

#[watcher_impl]
impl<'a> TransientWatcher<'a, ffi_ext::Ptr<wintypes::HWND>, ()> for WindowClosed<'a> {
    fn begin(win: ffi_ext::Ptr<wintypes::HWND>, sub: &'a ffi_ext::Subscription<()>) -> Self {
        let (pid, tid) = pid_tid(win.0);
        let hook = unsafe { winuser::SetWinEventHook(
            winuser::EVENT_OBJECT_DESTROY,
            winuser::EVENT_OBJECT_DESTROY,
            ptr::null_mut(),
            Some(WindowClosed::handler),
            pid, tid,
            winuser::WINEVENT_OUTOFCONTEXT) };
        WindowClosed { hwnd: win, sub, hook: ffi_ext::Ptr(hook) }
    }

    fn end(self) {
        completed!(self.sub);
        unsafe { winuser::UnhookWinEvent(self.hook.0); }
    }
}

impl<'a> WindowClosed<'a> {
    unsafe extern "system" fn handler(
        _win_event_hook: wintypes::HWINEVENTHOOK,
        _event: wintypes::DWORD,
        hwnd: wintypes::HWND,
        id_object: wintypes::LONG,
        id_child: wintypes::LONG,
        _id_event_thread: wintypes::DWORD,
        _dwms_event_time: wintypes::DWORD) {
        if id_object != winuser::OBJID_WINDOW || id_child != 0 { return; }
        let mut globals = transient_globals!(WindowClosed);
        let key = &ffi_ext::Ptr(hwnd);
        let closed = globals.get(key);
        if let Some(c) = &closed {
            next!(c.sub, &());
            completed!(c.sub);
            winuser::UnhookWinEvent(c.hook.0);
            globals.remove(key);
        }
    }
}
