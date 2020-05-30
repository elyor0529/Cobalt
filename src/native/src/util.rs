pub type FfiString = Vec<u16>;

pub struct Error {
    pub cause: FfiString
}

pub struct Subscription<T> {
    pub on_next: extern "cdecl" fn(&T),
    pub on_error: extern "cdecl" fn(Error),
    pub on_complete: extern "cdecl" fn(),
}

#[macro_use]
macro_rules! read_unicode_string {
    ($p: expr, $str: expr) => {{
        let mut buf_len = $str.Length as usize / 2;
        let mut buf = vec![0u16; buf_len];
        let res = winapi::um::memoryapi::ReadProcessMemory($p,
            $str.Buffer as *const std::ffi::c_void,
            buf.as_mut_ptr() as *mut _ as *mut std::ffi::c_void,
            buf_len * 2,
            &mut buf_len
        );
        if res == 0 { panic_win32!() };
        String::from_utf16_lossy(&buf)
    }};
}

macro_rules! read_struct {
    ($p: expr, $addr: expr, $typ: ty) => {{
        let mut ret: $typ = std::mem::zeroed();
        let res = winapi::um::memoryapi::ReadProcessMemory($p,
            $addr as *const std::ffi::c_void,
            &mut ret as *mut _ as *mut std::ffi::c_void,
            std::mem::size_of::<$typ>(),
            std::ptr::null_mut()
        );
        if res == 0 { panic_win32!() };
        ret
    }};
}

macro_rules! panic_win32 {
    () => {{
        panic!(std::io::Error::last_os_error().to_string())
    }}
}
