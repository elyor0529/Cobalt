#[macro_use]
pub type FfiString = Vec<u16>;

#[repr(C, u8)]
pub enum FfiResult<T> {
    Ok(T),
    Err(Error)
}

#[repr(C)]
pub struct Error {
    pub code: i32,
    pub cause: String
}

#[repr(C)]
pub struct Subscription<T> {
    pub on_next: extern "cdecl" fn(&T),
    pub on_error: extern "cdecl" fn(Error),
    pub on_complete: extern "cdecl" fn(),
}

impl<T> FfiResult<T> {
    pub fn get_last_err() -> FfiResult<T> {
        let err = std::io::Error::last_os_error();
        FfiResult::Err(Error { code: err.raw_os_error().unwrap(), cause: err.to_string() })
    }
}

impl<T> std::ops::Try for FfiResult<T>  {
    type Ok = T;
    type Error = Error;

    fn into_result(self) -> Result<T, Error> {
        match self {
            FfiResult::Ok(x) => Ok(x),
            FfiResult::Err(e) => Err(e)
        }
    }

    fn from_ok(v: T) -> Self {
        FfiResult::Ok(v)
    }

    fn from_error(e: Error) -> Self {
        FfiResult::Err(e)
    }
}

macro_rules! valid_bool {
    ($e: expr) => {{
        if $e { FfiResult::Ok(()) } else { FfiResult::get_last_err() }
    }};
}

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
