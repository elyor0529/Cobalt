#![feature(option_expect_none)]
#![feature(option_zip)]
#![feature(trivial_bounds)]
#![feature(try_trait)]
#![feature(osstring_ascii)]
#![feature(proc_macro_hygiene)]

#[macro_use]
extern crate lazy_static;

mod window;
mod process;

use ffi_ext::{next, err, completed};
use std::*;

// watcher that is a singleton (Rust side)
pub trait SingletonWatcher<'a, T> {
    fn begin(sub: &'a ffi_ext::Subscription<T>) -> Self;
    fn end(self);
}

// watcher that takes care of its own state (provided by C#)
pub trait StatefulWatcher<'a, T> {
    fn subscription(&'a self) -> &'a ffi_ext::Subscription<T>;
    fn begin(&'a mut self);
    fn end(self);
}

// watcher that needs its state to be managed using a global hashmap (Rust side)
pub trait TransientWatcher<'a, TA, TR> {
    fn begin(arg: TA, sub: &'a ffi_ext::Subscription<TR>) -> Self;
    fn end(self);
}

#[no_mangle]
pub fn range(start: u32, end: u32, sub: &ffi_ext::Subscription<u32>) {
    if end < start {
        err!(sub, "end cannot be before start");
    } else {
        for x in start..end {
            next!(sub, &x);
        }
    }
    completed!(sub);
}
