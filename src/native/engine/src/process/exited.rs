use crate::*;
use ffi::{windows::*, *};

#[repr(C)]
#[derive(Debug)]
pub struct ProcessExit<'a> {
    proc: wintypes::HANDLE,
    wait: *mut wintypes::c_void,
    sub: &'a ffi::Subscription<()>,
}

impl<'a> Watcher<'a, (), wintypes::HANDLE> for ProcessExit<'a> {
    fn new(sub: &'a mut Subscription<()>, proc: wintypes::HANDLE) -> Result<Self> {
        let mut ret = ProcessExit {
            proc,
            wait: ptr::null_mut(),
            sub,
        };
        expect!(true: winbase::RegisterWaitForSingleObject(
            &mut ret.wait,
            ret.proc,
            Some(ProcessExit::handler),
            &mut ret as *mut _ as *mut wintypes::c_void,
            winbase::INFINITE,
            winnt::WT_EXECUTEONLYONCE,
        ))?;
        ffi::Result::Ok(ret)
    }

    fn subscription(&'a self) -> &'a Subscription<()> {
        self.sub
    }
}

impl<'a> Drop for ProcessExit<'a> {
    fn drop(&mut self) {
        completed!(self.sub);
        unsafe { winbase::UnregisterWait(self.wait) };
    }
}

impl<'a> ProcessExit<'a> {
    pub unsafe extern "system" fn handler(dat: *mut wintypes::c_void, _: u8) {
        let inst = dat as *mut ProcessExit;
        (*inst).sub.next(&mut ());
        ptr::drop_in_place(inst);
    }
}

pub unsafe fn process_exit_watcher_begin<'a>(
    sub: &'a mut Subscription<()>,
    handle: wintypes::HANDLE,
    out: &'a mut ProcessExit<'a>,
) -> ffi::Status {
    write_out(ProcessExit::new(sub, handle), out)
}

pub unsafe fn process_exit_watcher_end(out: &mut ManuallyDrop<ProcessExit>) {
    ManuallyDrop::drop(out);
}
