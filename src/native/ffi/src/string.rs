pub use widestring::WideCStr as NulStr;
pub use widestring::WideCString as NulString;
pub use widestring::WideStr as Str;
pub use widestring::WideString as String;

#[macro_export]
macro_rules! buffer {
    ($sz: expr) => {
        vec![0u16; $sz as usize]
    };
}

#[macro_export]
macro_rules! buffer_to_string {
    ($buf: expr) => {{
        $crate::string::String::from_vec($buf)
    }};
    ($buf: expr, $len: expr) => {{
        unsafe { $buf.set_len($len as usize) };
        $crate::string::String::from_vec($buf)
    }};
}
