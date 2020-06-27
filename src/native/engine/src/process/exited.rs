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

    fn subscription(&'a self) -> &'a Subscription<()> { self.sub }

    fn begin(&'a mut self) {
        unsafe { winbase::RegisterWaitForSingleObject(
            &mut self.wait.0,
            self.proc.0, Some(ProcessExit::handler),
            self as *mut _ as *mut wintypes::c_void,
            winbase::INFINITE, winnt::WT_EXECUTEONLYONCE) };
    }

    fn end(self) { /* drop */ }

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
