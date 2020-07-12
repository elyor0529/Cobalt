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

// watcher that is a singleton (Rust side)
pub trait SingletonWatcher<'a, T> {
    fn begin(sub: &'a ffi::Subscription<T>) -> Self;
    fn end(self);
}

// watcher that takes care of its own state (provided by C#)
pub trait StatefulWatcher<'a, T> {
    fn subscription(&'a self) -> &'a ffi::Subscription<T>;
    fn begin(&'a mut self);
    fn end(self);
}

// watcher that needs its state to be managed using a global hashmap (Rust side)
pub trait TransientWatcher<'a, TA, TR> {
    fn begin(arg: TA, sub: &'a ffi::Subscription<TR>) -> Self;
    fn end(self);
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
