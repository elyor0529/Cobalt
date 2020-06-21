#![feature(option_zip)]
#![feature(trivial_bounds)]
#![feature(try_trait)]
#![feature(osstring_ascii)]
#![feature(proc_macro_hygiene)]

#[macro_use]
extern crate lazy_static;
#[macro_use]
mod ffiext;
mod win32;
mod window;
mod watchers;

use std::*;

#[no_mangle]
pub fn range(start: u32, end: u32, sub: &ffiext::Subscription<u32>) {
    if end < start {
        err!(sub, "end cannot be before start");
    } else {
        for x in start..end {
            next!(sub, &x);
        }
    }
    completed!(sub);
}
