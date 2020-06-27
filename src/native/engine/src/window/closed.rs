use proc_macros::*;
use ffi_ext::{completed, next};
use ffi_ext::win32::*;
use watchers::*;
use std::*;
use crate::*;

#[repr(C)]
pub struct WindowClosed<'a> {
    pub hwnd: ffi_ext::Ptr<windef::HWND>,
    pub hook: ffi_ext::Ptr<windef::HWINEVENTHOOK>,
    pub sub: &'a ffi_ext::Subscription<()>,
}

#[watcher_impl]
impl<'a> TransientWatcher<'a, ffi_ext::Ptr<windef::HWND>, ()> for WindowClosed<'a> {
    fn begin(win: ffi_ext::Ptr<windef::HWND>, sub: &'a ffi_ext::Subscription<()>) -> Self {
        let mut pid = 0;
        let tid = unsafe { winuser::GetWindowThreadProcessId(win.0, &mut pid) };
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
        _win_event_hook: windef::HWINEVENTHOOK,
        _event: minwindef::DWORD,
        hwnd: windef::HWND,
        id_object: winnt::LONG,
        id_child: winnt::LONG,
        _id_event_thread: minwindef::DWORD,
        _dwms_event_time: minwindef::DWORD) {
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
