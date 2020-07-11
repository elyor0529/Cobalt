use crate::windows::*;
use crate::*;

pub struct WinEventHook {
    hook: wintypes::HWINEVENTHOOK,
}

impl WinEventHook {
    pub fn new(
        event_min: wintypes::DWORD,
        event_max: wintypes::DWORD,
        hmod_win_event_proc: wintypes::HMODULE,
        pfn_win_event_proc: winuser::WINEVENTPROC,
        id_process: wintypes::DWORD,
        id_thread: wintypes::DWORD,
        dw_flags: wintypes::DWORD,
    ) -> result::Result<Self> {
        let hook = expect!(non_null: {
            winuser::SetWinEventHook(
                event_min,
                event_max,
                hmod_win_event_proc,
                pfn_win_event_proc,
                id_process,
                id_thread,
                dw_flags,
            )
        })?;
        result::Result::Ok(WinEventHook { hook })
    }
}

impl Drop for WinEventHook {
    fn drop(&mut self) {
        expect!(true: winuser::UnhookWinEvent(self.hook)).unwrap(); // TODO instead of unwrap, something else?
    }
}
