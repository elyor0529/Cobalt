use crate::windows::*;
use libffi::high::*;

pub struct WinEventHook<'a> {
    hook: wintypes::HWINEVENTHOOK,
    _callback: Callback<'a>
}
unsafe impl<'a> Send for WinEventHook<'a> {}
unsafe impl<'a> Sync for WinEventHook<'a> {}

pub trait WinEventCallbackFn = Fn(wintypes::HWINEVENTHOOK, wintypes::DWORD, wintypes::HWND, wintypes::LONG, wintypes::LONG, wintypes::DWORD, wintypes::DWORD) -> ();

struct Callback<'a> {
    _closure: Box<dyn WinEventCallbackFn + 'a>,
    ffi: Closure7<'a, wintypes::HWINEVENTHOOK, wintypes::DWORD, wintypes::HWND, wintypes::LONG, wintypes::LONG, wintypes::DWORD, wintypes::DWORD, ()>,
}

impl<'a> Callback<'a> {
    fn new<F>(closure: F) -> Callback<'a>
        where F: 'a + WinEventCallbackFn {
        let _closure = Box::new(closure);
        let ffi = Closure7::new::<F>( unsafe { mem::transmute(&*_closure) });
        Callback { _closure, ffi }
    }

    fn ptr(&self) -> winuser::WINEVENTPROC {
        Some(unsafe { std::mem::transmute(*self.ffi.code_ptr()) })
    }
}

#[repr(u32)]
#[derive(Clone, Copy)]
pub enum WinEvent {
    SystemForeground = winuser::EVENT_SYSTEM_FOREGROUND,
    ObjectDestroyed = winuser::EVENT_OBJECT_DESTROY,
}

pub enum Locality {
    Global,
    ProcessThread { pid: u32, tid: u32 },
}

impl<'a> WinEventHook<'a> {
    pub fn new<F>(
        ev: WinEvent,
        locality: Locality,
        handler: F
    ) -> Result<Self, crate::windows::WinError> where F: 'a + WinEventCallbackFn {

        let (event_min, event_max) = (ev as u32, ev as u32);
        let (id_process, id_thread) = match locality {
            Locality::Global => (0, 0),
            Locality::ProcessThread { pid, tid } => (pid, tid),
        };
        let _callback = Callback::new(handler);

        let hook = expect!(non_null: {
            winuser::SetWinEventHook(
                event_min,
                event_max,
                ptr::null_mut(),
                _callback.ptr(),
                id_process,
                id_thread,
                winuser::WINEVENT_OUTOFCONTEXT,
            )
        })?;
        Ok(WinEventHook { hook, _callback })
    }
}

impl<'a> Drop for WinEventHook<'a> {
    fn drop(&mut self) {
        expect!(true: winuser::UnhookWinEvent(self.hook)).unwrap();
    }
}
