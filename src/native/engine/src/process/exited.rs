use crate::*;
use ffi::win32::*;
use ffi::{completed, next, Subscription};
use proc_macros::*;
use std::*;

#[repr(C)]
#[derive(Debug)]
pub struct ProcessExit<'a> {
    proc: ffi::Ptr<wintypes::HANDLE>,
    wait: ffi::Ptr<*mut wintypes::c_void>,
    sub: &'a ffi::Subscription<()>,
}

#[watcher_impl]
impl<'a> StatefulWatcher<'a, ()> for ProcessExit<'a> {
    fn subscription(&'a self) -> &'a Subscription<()> {
        self.sub
    }

    fn begin(&'a mut self) {
        unsafe {
            winbase::RegisterWaitForSingleObject(
                &mut self.wait.0,
                self.proc.0,
                Some(ProcessExit::handler),
                self as *mut _ as *mut wintypes::c_void,
                winbase::INFINITE,
                winnt::WT_EXECUTEONLYONCE,
            )
        };
    }

    fn end(self) { /* drop */
    }
}

impl<'a> Drop for ProcessExit<'a> {
    fn drop(&mut self) {
        completed!(self.sub);
        unsafe { winbase::UnregisterWait(self.wait.0) };
    }
}

impl<'a> ProcessExit<'a> {
    pub unsafe extern "system" fn handler(dat: *mut wintypes::c_void, _: u8) {
        let inst = dat as *mut ProcessExit;
        next!((*inst).sub, &());
        ptr::drop_in_place(inst);
    }
}
