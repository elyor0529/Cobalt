use winapi::um::*;
use winapi::shared::*;
use crate::util::*;

#[no_mangle]
pub unsafe fn window_title(hwnd: windef::HWND) -> FfiString {
    let len = winuser::GetWindowTextLengthW(hwnd);
    let mut buf = vec![0u16; len as usize + 1];
    winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), len + 1);
    buf
}
