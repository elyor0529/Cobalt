pub mod foreground_window_watcher;

use winapi::um::*;
use std::*;

#[no_mangle]
pub unsafe fn event_loop_step() {
    let mut msg: winuser::MSG = std::mem::zeroed();
    if 0 == winuser::GetMessageW(&mut msg, ptr::null_mut(), 0,0) { return }
    winuser::TranslateMessage(&mut msg);
    winuser::DispatchMessageW(&mut msg);
}
