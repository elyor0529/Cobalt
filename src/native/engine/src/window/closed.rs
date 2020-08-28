use crate::*;
use ffi::{windows::*, *};
use lazy_static::*;
use std::collections::HashMap;
use std::sync::Mutex;

#[derive(Debug, Eq, PartialEq, Ord, PartialOrd, Hash, Copy, Clone)]
pub struct WinHandle(pub wintypes::HWND);
unsafe impl Send for WinHandle {}
unsafe impl Sync for WinHandle {}

#[repr(C)]
#[derive(Debug)]
pub struct WindowClosed<'a> {
    pub hwnd: WinHandle,
    pub hook: WinEventHook,
    pub sub: &'a mut ffi::Subscription<()>,
}

impl<'a> Watcher<'a, (), WinHandle> for WindowClosed<'a> {
    fn new(sub: &'a mut ffi::Subscription<()>, hwnd: WinHandle) -> ffi::Result<Self> {
        let (pid, tid) = window::Window::pid_tid(hwnd.0)?;

        let hook = WinEventHook::new(
            EventRange::Single(WinEvent::ObjectDestroyed),
            EventLocality::ProcessThread { pid, tid },
            Some(WindowClosed::handler),
        )?;

        ffi::Result::Ok(WindowClosed { hwnd, hook, sub })
    }

    fn subscription(&self) -> &Subscription<()> {
        self.sub
    }
}

impl<'a> Drop for WindowClosed<'a> {
    fn drop(&mut self) {
        self.subscription().complete();
    }
}

lazy_static! {
    static ref WINDOW_CLOSED_GLOBALS: Mutex<HashMap<WinHandle, &'static WindowClosed<'static>>> =
        Mutex::new(HashMap::new());
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
        if let Some(c) = WINDOW_CLOSED_GLOBALS
            .lock()
            .unwrap()
            .remove(&WinHandle(hwnd))
        {
            c.sub.next(&mut ());
            drop(c);
        } else {
        }
    }
}

#[no_mangle]
pub unsafe fn window_closed_watcher_begin(
    watcher: &'static mut MaybeUninit<WindowClosed<'static>>,
    hwnd: WinHandle,
    sub: &'static mut ffi::Subscription<()>,
) -> ffi::Status {
    match WindowClosed::new(sub, hwnd) {
        Ok(res) => {
            let w = watcher.write(res);
            WINDOW_CLOSED_GLOBALS.lock().unwrap().insert(w.hwnd, w);
            ffi::Status::Success
        }
        Err(e) => e.into(),
    }
}

#[no_mangle]
pub unsafe fn window_closed_watcher_end(watcher: &mut ffi::ManuallyDrop<WindowClosed>) {
    WINDOW_CLOSED_GLOBALS.lock().unwrap().remove(&watcher.hwnd);
    ffi::ManuallyDrop::drop(watcher);
}
