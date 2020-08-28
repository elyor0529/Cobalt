use crate::windows::*;
use crate::*;

#[derive(Debug)]
pub struct WinEventHook {
    hook: wintypes::HWINEVENTHOOK,
}
unsafe impl Send for WinEventHook {}
unsafe impl Sync for WinEventHook {}

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

impl WinEventHook {
    pub fn new(
        event_range: EventRange,
        event_locality: EventLocality,
        pfn_win_event_proc: winuser::WINEVENTPROC,
    ) -> result::Result<Self> {
        let (event_min, event_max) = match event_range {
            EventRange::Single(ev) => (ev as u32, ev as u32),
            EventRange::Range(ev_min, ev_max) => (ev_min as u32, ev_max as u32),
        };
        let (id_process, id_thread) = match event_locality {
            EventLocality::Global => (0, 0),
            EventLocality::ProcessThread { pid, tid } => (pid, tid),
        };
        let hook = expect!(non_null: {
            winuser::SetWinEventHook(
                event_min,
                event_max,
                ptr::null_mut(),
                pfn_win_event_proc,
                id_process,
                id_thread,
                winuser::WINEVENT_OUTOFCONTEXT,
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
