use crate::*;
use win32::*;
use casey::{snake};

pub trait SingleInstanceWatcher<'a, T> {
    fn begin(sub: &'a ffiext::Subscription<u32>) -> Self;
    fn end(self); 
    fn subscription(&self) -> &ffiext::Subscription<T>;
}

// nope need proc_macros for this
macro_rules! watcher {
    (single $t:ident) => [
        pub static mut concat_idents!($t,_INSTANCE): Option<$t> = None;
    ];
}

pub struct ForegroundWindowWatcher<'a> {
    pub hook: windef::HWINEVENTHOOK,
    pub sub: &'a ffiext::Subscription<u32>
}

impl<'a> SingleInstanceWatcher<'a, u32> for ForegroundWindowWatcher<'a> {

    #[inline(always)]
    fn subscription(&self) -> &ffiext::Subscription<u32> { self.sub }

    fn begin(sub: &'a ffiext::Subscription<u32>) -> Self {
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
    let sub = FOREGROUND_WINDOW_WATCHER_INSTANCE.as_ref().expect("FOREGROUND_WINDOW_WATCHER_INSTANCE should be already initialized").subscription();
    err!(sub, "lol wat");
    
}

pub static mut FOREGROUND_WINDOW_WATCHER_INSTANCE: Option<ForegroundWindowWatcher> = None;

// TODO these need to return Results

#[no_mangle]
pub unsafe fn foreground_window_watcher_begin(sub: &'static ffiext::Subscription<u32>) {
    FOREGROUND_WINDOW_WATCHER_INSTANCE = Some(ForegroundWindowWatcher::begin(sub));
}

#[no_mangle]
pub unsafe fn foreground_window_watcher_end() {
    FOREGROUND_WINDOW_WATCHER_INSTANCE.take().unwrap().end();
}
