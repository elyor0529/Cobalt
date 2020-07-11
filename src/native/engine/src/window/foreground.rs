use crate::*;
use ffi::windows::*;
use ffi::*;
use proc_macros::*;

#[repr(C)]
#[derive(Debug)]
pub struct ForegroundWindowSwitch {
    pub win: window::Basic,
    pub filetime_ticks: i64,
}

#[derive(Debug)]
pub struct ForegroundWindowWatcher<'a> {
    pub sub: &'a ffi::Subscription<ForegroundWindowSwitch>,
    pub hook: wintypes::HWINEVENTHOOK,
}

// #[watcher_impl]
impl<'a> SingletonWatcher<'a, ForegroundWindowSwitch> for ForegroundWindowWatcher<'a> {
    fn begin(sub: &'a ffi::Subscription<ForegroundWindowSwitch>) -> Self {
        let hook = unsafe {
            winuser::SetWinEventHook(
                winuser::EVENT_SYSTEM_FOREGROUND,
                winuser::EVENT_SYSTEM_FOREGROUND,
                ptr::null_mut(),
                Some(ForegroundWindowWatcher::handler),
                0,
                0,
                winuser::WINEVENT_OUTOFCONTEXT,
            )
        };
        ForegroundWindowWatcher { hook, sub }
    }

    fn end(self) {
        completed!(self.sub);
        unsafe {
            winuser::UnhookWinEvent(self.hook);
        }
    }
}

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
        let ForegroundWindowWatcher { sub, .. } = singleton_instance!(ForegroundWindowWatcher);

        let title = window::title(hwnd);
        let filetime_ticks = Ticks(dwms_event_time).filetime();
        let win = window::Basic { hwnd, title };
        let fg_switch = ForegroundWindowSwitch {
            win,
            filetime_ticks,
        };
        next!(sub, &fg_switch);
    }
}
