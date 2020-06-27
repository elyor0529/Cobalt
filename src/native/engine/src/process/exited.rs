use proc_macros::*;
use ffi_ext::win32::*;
use ffi_ext::{next, completed, Subscription};
use std::*;
use crate::*;

#[repr(C)]
#[derive(Debug)]
pub struct ProcessExit<'a> {
    proc: ffi_ext::Ptr<wintypes::HANDLE>,
    wait: ffi_ext::Ptr<*mut wintypes::c_void>,
    sub: &'a ffi_ext::Subscription<()>
}

#[watcher_impl]
impl<'a> StatefulWatcher<'a, ()> for ProcessExit<'a> {
    fn subscription(&'a self) -> &'a Subscription<()> {
        self.sub
    }

    fn begin(&'a mut self) {
        unsafe { winbase::RegisterWaitForSingleObject(
            &mut self.wait.0,
            self.proc.0, Some(ProcessExit::handler),
            self as *mut _ as *mut ctypes::c_void,
            winbase::INFINITE, winnt::WT_EXECUTEONLYONCE) };
    }

    fn end(self) {
        unimplemented!()
    }
}

impl<'a> ProcessExit<'a> {
    pub unsafe extern "system" fn handler(dat: *mut ctypes::c_void, _: u8) {
        let ProcessExit { sub, wait, .. } = &mut *(dat as *mut ProcessExit);
        next!(sub, &());
        completed!(sub);
        winbase::UnregisterWait(wait.0);
    }
}
