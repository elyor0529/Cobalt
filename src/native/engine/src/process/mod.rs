use ffi_ext::win32::*;
use std::*;
use crate::*;

#[repr(C)]
#[derive(Debug)]
pub struct Basic {
    pub id: u32
}

#[repr(C)]
#[derive(Debug)]
pub struct Extended {
    handle: *mut ctypes::c_void,
    path: ffi_ext::String,
    cmd_line: ffi_ext::String,
    name: ffi_ext::String,
    description: ffi_ext::String
}

#[no_mangle]
pub fn process_basic(hwnd: wintypes::HWND) -> Basic {
    Basic { id: window::pid_tid(hwnd).0 }
}

