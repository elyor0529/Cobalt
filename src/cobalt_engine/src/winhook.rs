use winapi::um::*;
use winapi::shared::windef::*;
use winapi::shared::minwindef::*;
use winapi::shared::windef::*;
use winapi::um::winnt::*;
use ntapi::*;
use std::*;
use libffi::high::*;

pub struct WinEventHook<'a> {
    hook: HWINEVENTHOOK,
    callback: Callback<'a>
}
unsafe impl<'a> Send for WinEventHook<'a> {}
unsafe impl<'a> Sync for WinEventHook<'a> {}

struct Callback<'a> {
    closure: Box<dyn Fn(HWINEVENTHOOK, DWORD, HWND, LONG, LONG, DWORD, DWORD) -> () + 'a>,
    ffi: Closure7<'a, HWINEVENTHOOK, DWORD, HWND, LONG, LONG, DWORD, DWORD, ()>,
}

impl<'a> Callback<'a> {
    fn new<F>(closure: F) -> Callback<'a>
        where F: 'a + Fn(HWINEVENTHOOK, DWORD, HWND, LONG, LONG, DWORD, DWORD) -> () {
        let closure = Box::new(closure);
        let ffi = Closure7::new::<F>( unsafe { mem::transmute(&*closure) });
        Callback { closure, ffi }
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

pub enum EventRange {
    Single(WinEvent),
    Range(WinEvent, WinEvent),
}

pub enum EventLocality {
    Global,
    ProcessThread { pid: u32, tid: u32 },
}

impl<'a> WinEventHook<'a> {
    pub fn new<F>(
        event_range: EventRange,
        event_locality: EventLocality,
        handler: F
    ) -> Self where F: 'a + Fn(HWINEVENTHOOK, DWORD, HWND, LONG, LONG, DWORD, DWORD) -> () {

        let (event_min, event_max) = match event_range {
            EventRange::Single(ev) => (ev as u32, ev as u32),
            EventRange::Range(ev_min, ev_max) => (ev_min as u32, ev_max as u32),
        };
        let (id_process, id_thread) = match event_locality {
            EventLocality::Global => (0, 0),
            EventLocality::ProcessThread { pid, tid } => (pid, tid),
        };
        let callback = Callback::new(handler);

        let hook = unsafe {
            winuser::SetWinEventHook(
                event_min,
                event_max,
                ptr::null_mut(),
                callback.ptr(),
                id_process,
                id_thread,
                winuser::WINEVENT_OUTOFCONTEXT,
            )
        }; // TODO check this (expect non-null)
        WinEventHook { hook, callback }
    }
}

impl<'a> Drop for WinEventHook<'a> {
    fn drop(&mut self) {
        unsafe { winuser::UnhookWinEvent(self.hook) }; // TODO check this (expect true)
    }
}

