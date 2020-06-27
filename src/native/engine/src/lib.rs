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
mod watchers;

use std::*;

#[no_mangle]
pub fn range(start: u32, end: u32, sub: &ffi_ext::Subscription<u32>) {
    if end < start {
        ffi_ext::err!(sub, "end cannot be before start");
    } else {
        for x in start..end {
            ffi_ext::next!(sub, &x);
        }
    }
    ffi_ext::completed!(sub);
}
