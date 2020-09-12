use crate::windows::*;
use std::task::*;
use std::future::*;

pub struct EventLoop {
    msg: winuser::MSG,
}

impl EventLoop {
    pub fn new() -> EventLoop {
        EventLoop { msg: Default::default() }
    }
}

impl Future for EventLoop {
    type Output = usize;
    fn poll(mut self: std::pin::Pin<&mut Self>, cx: &mut Context<'_>) -> Poll<<Self as Future>::Output> {
        while unsafe {
            winuser::PeekMessageW(
                &mut self.msg,
                ptr::null_mut(), 0, 0,
                winuser::PM_REMOVE) } != 0
        {
            if self.msg.message == winuser::WM_QUIT {
                return Poll::Ready(self.msg.wParam);
            }
            unsafe { winuser::TranslateMessage(&mut self.msg as *mut _) };
            unsafe { winuser::DispatchMessageW(&mut self.msg as *mut _) };
        }
        cx.waker().wake_by_ref(); // yield to scheduler
        Poll::Pending
    }
}

