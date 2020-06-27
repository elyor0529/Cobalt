use ffi_ext::win32::*;
use std::*;

mod foreground;
mod closed;

#[repr(C)]
#[derive(Debug)]
pub struct Basic {
    pub hwnd: windef::HWND,
    pub title: ffi_ext::String
}

#[repr(C)]
#[derive(Debug)]
pub struct Extended {
    // TODO put pid?
    uwp_aumid: ffi_ext::Nullable<ffi_ext::String>
}

#[no_mangle]
pub unsafe fn window_title(hwnd: windef::HWND) -> ffi_ext::String {
    let len = winuser::GetWindowTextLengthW(hwnd);
    let mut buf = vec![0u16; len as usize+1];
    winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), len+1);
    buf.set_len(len as usize); // Do not include the u16 null byte at the end
    ffi_ext::String::from_vec(buf)
}

