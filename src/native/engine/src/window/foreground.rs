use crate::*;
use ffi::{windows::*, *};

#[repr(C)]
#[derive(Debug)]
pub struct ForegroundWindowSwitch {
    pub window: window::Window,
    pub filetime_ticks: i64,
}

#[derive(Debug)]
pub struct ForegroundWindowWatcher<'a> {
    pub sub: &'a mut ffi::Subscription<ForegroundWindowSwitch>,
    pub hook: WinEventHook,
}

impl<'a> Watcher<'a, ForegroundWindowSwitch, ()> for ForegroundWindowWatcher<'a> {
    fn new(sub: &'a mut ffi::Subscription<ForegroundWindowSwitch>, _: ()) -> ffi::Result<Self> {
        let hook = WinEventHook::new(
            EventRange::Single(WinEvent::SystemForeground),
            EventLocality::Global,
            Some(ForegroundWindowWatcher::handler),
        )?;
        ffi::Result::Ok(ForegroundWindowWatcher { hook, sub })
    }

    fn subscription(&self) -> &Subscription<ForegroundWindowSwitch> {
        self.sub
    }
}

impl<'a> Drop for ForegroundWindowWatcher<'a> {
    fn drop(&mut self) {
        self.subscription().complete()
    }
}

pub static mut FOREGROUND_WINDOW_INSTANCE: std::option::Option<
    &'static mut ForegroundWindowWatcher<'static>,
> = None;

impl<'a> ForegroundWindowWatcher<'a> {
    unsafe extern "system" fn handler(
        _win_event_hook: wintypes::HWINEVENTHOOK,
        _event: wintypes::DWORD,
        hwnd: wintypes::HWND,
        _id_object: wintypes::LONG,
        _id_child: wintypes::LONG,
        _id_event_thread: wintypes::DWORD,
        dwms_event_time: wintypes::DWORD,
    ) {
        if winuser::IsWindow(hwnd) == 0 || winuser::IsWindowVisible(hwnd) == 0 {
            return;
        }

        let ForegroundWindowWatcher { sub, .. } = FOREGROUND_WINDOW_INSTANCE.as_ref().unwrap();

        let title = window::Window::title(hwnd);
        let filetime_ticks = Ticks(dwms_event_time).filetime();
        let window = window::Basic { hwnd, title }.into();

        sub.next(&mut ForegroundWindowSwitch {
            window,
            filetime_ticks,
        });
    }
}

#[no_mangle]
pub unsafe fn foreground_window_watcher_begin(
    watcher: &'static mut MaybeUninit<window::foreground::ForegroundWindowWatcher<'static>>,
    sub: &'static mut ffi::Subscription<ForegroundWindowSwitch>,
) -> ffi::Status {
    match ForegroundWindowWatcher::new(sub, ()) {
        Ok(res) => {
            FOREGROUND_WINDOW_INSTANCE = Some(watcher.write(res));
            ffi::Status::Success
        }
        Err(e) => e.into(),
    }
}

#[no_mangle]
pub unsafe fn foreground_window_watcher_end(
    watcher: &mut ffi::ManuallyDrop<ForegroundWindowWatcher>,
) {
    FOREGROUND_WINDOW_INSTANCE = None;
    ffi::ManuallyDrop::drop(watcher);
}
