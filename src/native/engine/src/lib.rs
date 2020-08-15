#![feature(option_expect_none)]
#![feature(option_zip)]
#![feature(trivial_bounds)]
#![feature(try_trait)]
#![feature(osstring_ascii)]
#![feature(proc_macro_hygiene)]
#![feature(maybe_uninit_extra)]
#![feature(maybe_uninit_ref)]

pub mod process;
pub mod window;

use ffi::*;

pub trait Watcher<'a, T, TA>: Sized + Drop {
    fn new(sub: &'a mut Subscription<T>, arg: TA) -> ffi::Result<Self>;
    fn subscription(&'a self) -> &'a Subscription<T>;
}

#[no_mangle]
pub fn range(start: u32, end: u32, sub: &ffi::Subscription<u32>) {
    if end < start {
        err!(sub, "end cannot be before start");
    } else {
        for mut x in start..end {
            next!(sub, &mut x);
        }
    }
    completed!(sub);
}
