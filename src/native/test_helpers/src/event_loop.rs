use ffi::{windows::*, *};
use std::sync::{Arc, Mutex};

// maybe move this to engine?
pub struct EventLoop {
    cancel: bool,
}

impl EventLoop {
    pub fn new() -> EventLoop {
        EventLoop { cancel: false }
    }

    pub fn step(&self) -> std::option::Option<usize> {
        let mut msg: winuser::MSG = Default::default();
        while unsafe { winuser::PeekMessageW(&mut msg, ptr::null_mut(), 0, 0, winuser::PM_REMOVE) }
            != 0
        {
            if msg.message == winuser::WM_QUIT {
                return Some(msg.wParam);
            }
            unsafe { winuser::TranslateMessage(&mut msg as *mut _) };
            unsafe { winuser::DispatchMessageW(&mut msg as *mut _) };
        }
        None
    }

    pub fn run(s: Arc<Mutex<EventLoop>>) -> std::option::Option<usize> {
        while !s.lock().unwrap().cancel {
            if let Some(ret) = s.lock().unwrap().step() {
                return Some(ret);
            }
        }
        None
    }

    pub fn cancel(&mut self) {
        self.cancel = true;
    }
}
