#[macro_use]
macro_rules! read_unicode_string {
    ($p: expr, $str: expr) => {{
        let mut buf_len = $str.Length as usize / 2;
        let mut buf = vec![0u16; buf_len];
        let res = memoryapi::ReadProcessMemory($p,
            $str.Buffer as *const ffi::c_void,
            buf.as_mut_ptr() as *mut _ as *mut ffi::c_void,
            buf_len * 2,
            &mut buf_len
        );
        if res == 0 { panic!(std::io::Error::last_os_error().to_string()) };
        String::from_utf16_lossy(&buf)
    }};
}

macro_rules! read_struct {
    ($p: expr, $addr: expr, $typ: ty) => {{
        let mut ret: $typ = std::mem::zeroed();
        let res = memoryapi::ReadProcessMemory($p,
            $addr as *const ffi::c_void,
            &mut ret as *mut _ as *mut ffi::c_void,
            mem::size_of::<$typ>(),
            ptr::null_mut()
        );
        if res == 0 { panic!(std::io::Error::last_os_error().to_string()) };
        ret
    }};
}

macro_rules! panic_win32 {
    () => {{
        panic!(std::io::Error::last_os_error().to_string())
    }}
}
