#![feature(option_zip)]
#![feature(try_trait)]
#![feature(osstring_ascii)]
#[macro_use]
extern crate lazy_static;
#[macro_use]
mod util;
mod info;
mod watchers;

use crate::util::*;

#[no_mangle]
pub unsafe fn add() -> FfiResult<u32> {
    FfiResult::Err(Error { code: 05, cause: String::from("what") })
}
