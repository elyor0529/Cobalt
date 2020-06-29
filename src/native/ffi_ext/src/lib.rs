use std::*;

pub mod win32;

pub use widestring::WideString as String;
pub use widestring::WideStr as Str;
pub use widestring::WideCString as NulString;
pub use widestring::WideCStr as NulStr;

#[macro_export]
macro_rules! buffer {
    ($sz: expr) => {
        vec![0u16; $sz as usize]
    };
}

#[macro_export]
macro_rules! buffer_to_string {
    ($buf: expr) => {
        ffi_ext::String::from_vec($buf)
    }
}

#[repr(C)]
#[derive(Ord, PartialOrd, Eq, PartialEq, Hash)]
pub struct Ptr<T>(pub T);

unsafe impl<T> Send for Ptr<T> {}
unsafe impl<T> Sync for Ptr<T> {}

impl<T> Ptr<extern "cdecl" fn(&T)> {
    #[inline(always)]
    pub fn call(&self, v: &T) {
        (&self.0)(v)
    }
}

impl<T> fmt::Debug for Ptr<extern "cdecl" fn(&T)> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{:p}", &self.0)
    }
}
impl<T> fmt::Debug for Ptr<*mut T> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{:p}", &self.0)
    }
}
impl<T> fmt::Debug for Ptr<*const T> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{:p}", &self.0)
    }
}

#[repr(C, u64)]
#[derive(Ord, PartialOrd, Eq, PartialEq, Hash)]
pub enum Error {
    Win32(u32),
    Custom(String)
}

#[repr(C, u64)]
#[derive(Debug, Ord, PartialOrd, Eq, PartialEq, Hash)]
pub enum Option<T> {
    Some(T),
    None
}

impl<T> From<option::Option<T>> for Option<T> {
    fn from(opt: option::Option<T>) -> Self {
        match opt {
            Some(x) => Option::Some(x),
            None => Option::None
        }
    }
}

#[repr(C)]
#[derive(Debug)]
pub struct Subscription<T> {
    pub on_next: Ptr<extern "cdecl" fn(&T)>,
    pub on_error: Ptr<extern "cdecl" fn(&Error)>,
    pub on_complete: Ptr<extern "cdecl" fn(&())>,
}

#[macro_export]
macro_rules! next {
    ($sub: expr, $e: expr) => {
        $sub.on_next.call($e);
    };
}

#[macro_export]
macro_rules! err {
    ($sub: expr, $s: expr) => {
        $sub.on_error.call(&ffi_ext::Error::Custom(ffi_ext::String::from_str($s)));
    };
}

#[macro_export]
macro_rules! completed {
    ($sub: expr) => {
        $sub.on_complete.call(&());
    };
}

#[macro_export]
macro_rules! read_unicode_string {
    ($p: expr, $str: expr) => {{
        let mut buf_len = $str.Length as usize / 2;
        let mut buf = ffi_ext::buffer!(buf_len);
        let res = winapi::um::memoryapi::ReadProcessMemory($p.0,
            $str.Buffer as *mut _ as *mut winapi::ctypes::c_void,
            buf.as_mut_ptr() as *mut _ as *mut winapi::ctypes::c_void,
            buf_len * 2,
            &mut buf_len
        );
        if res == 0 { ffi_ext::panic_win32!() };
        ffi_ext::buffer_to_string!(buf)
    }};
}

#[macro_export]
macro_rules! read_struct {
    ($p: expr, $addr: expr, $typ: ty) => {{
        let mut ret: $typ = std::mem::zeroed();
        let res = winapi::um::memoryapi::ReadProcessMemory($p.0,
            $addr as *mut _ as *mut winapi::ctypes::c_void,
            &mut ret as *mut _ as *mut winapi::ctypes::c_void,
            std::mem::size_of::<$typ>(),
            std::ptr::null_mut()
        );
        if res == 0 { ffi_ext::panic_win32!() };
        ret
    }};
}

#[macro_export]
macro_rules! panic_win32 {
    () => {{
        panic!(std::io::Error::last_os_error().to_string())
    }}
}

#[cfg(test)]
mod tests {
    use super::*;

    extern "cdecl" fn sub_on_next(x: &u32) {
        println!("what {}", x);
    }
    extern "cdecl" fn sub_on_error(_: &Error) {}
    extern "cdecl" fn sub_on_compl(_: &()) {}

    #[test]
    fn subscription_debug() {
        let sub = Subscription { on_next: Ptr(sub_on_next), on_error: Ptr(sub_on_error), on_complete: Ptr(sub_on_compl) };
        let re = regex::Regex::new(r"Subscription \{ on_next: 0x[[:xdigit:]]+, on_error: 0x[[:xdigit:]]+, on_complete: 0x[[:xdigit:]]+ \}").unwrap();
        let out = format!("{:?}", sub);
        println!("{}", out);
        sub.on_next.call(&1);
        assert!(re.is_match(out.as_str()));
    }
}
