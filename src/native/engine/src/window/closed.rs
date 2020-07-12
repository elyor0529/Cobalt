use crate::*;
use ffi::{self::*, windows::*};
use proc_macros::*;

#[repr(C)]
#[derive(Debug)]
pub struct WindowClosed<'a> {
    pub hwnd: ffi::Ptr<wintypes::HWND>,
    pub hook: ffi::Ptr<wintypes::HWINEVENTHOOK>,
    pub sub: &'a ffi::Subscription<()>,
}

#[watcher_impl]
impl<'a> TransientWatcher<'a, ffi::Ptr<wintypes::HWND>, ()> for WindowClosed<'a> {
    fn begin(win: ffi::Ptr<wintypes::HWND>, sub: &'a ffi::Subscription<()>) -> Self {
        let (pid, tid) = crate::window::pid_tid(win.0);
        let hook = unsafe {
            winuser::SetWinEventHook(
                winuser::EVENT_OBJECT_DESTROY,
                winuser::EVENT_OBJECT_DESTROY,
                ptr::null_mut(),
                Some(WindowClosed::handler),
                pid,
                tid,
                winuser::WINEVENT_OUTOFCONTEXT,
            )
        };
        WindowClosed {
            hwnd: win,
            sub,
            hook: ffi::Ptr(hook),
        }
    }

    fn end(self) {
        completed!(self.sub);
        unsafe {
            winuser::UnhookWinEvent(self.hook.0);
        }
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
        _dwms_event_time: wintypes::DWORD,
    ) {
        if id_object != winuser::OBJID_WINDOW || id_child != 0 {
            return;
        }
        let mut globals = transient_globals!(WindowClosed);
        let key = &ffi::Ptr(hwnd);
        let closed = globals.get(key);
        if let Some(c) = &closed {
            next!(c.sub, &());
            completed!(c.sub);
            winuser::UnhookWinEvent(c.hook.0);
            globals.remove(key);
        }
    }
}
