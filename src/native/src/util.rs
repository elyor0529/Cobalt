use safer_ffi::prelude::*;
use widestring::*;
use std::*;

pub struct FnPtr<T>(extern "cdecl" fn(T));
impl<T> fmt::Debug for FnPtr<T> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{:p}", &self.0)
    }
}

#[repr(C, u8)]
#[derive(Debug)]
pub enum Error {
    Win32(u32),
    Custom(WideString)
}

#[repr(C, u8)]
#[derive(Debug)]
pub enum Result<T> {
    Ok(T),
    Err(Error)
}

#[repr(C, u8)]
#[derive(Debug)]
pub enum Nullable<T> {
    Value(T),
    Null
}
impl<T> From<Option<T>> for Nullable<T> {
    fn from(opt: Option<T>) -> Self {
        match opt {
            Some(x) => Nullable::Value(x),
            None => Nullable::Null
        }
    }
}

#[derive_ReprC]
#[repr(C)]
pub struct Subscription<T> {
    pub on_next: extern "cdecl" fn(&T),
    pub on_error: extern "cdecl" fn(Error),
    pub on_complete: extern "cdecl" fn(()),
}
impl<T> fmt::Debug for Subscription<T> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        f.debug_struct("Subscription")
            .field("on_next", &FnPtr(self.on_next))
            .field("on_error", &FnPtr(self.on_error))
            .field("on_complete", &FnPtr(self.on_complete))
            .finish()
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    extern "cdecl" fn sub_on_next(_: &u32) {}
    extern "cdecl" fn sub_on_error(_: Error) {}
    extern "cdecl" fn sub_on_compl(_: ()) {}

    #[test]
    fn subscription_debug() {
        let sub = Subscription { on_next: sub_on_next, on_error: sub_on_error, on_complete: sub_on_compl };
        let re = regex::Regex::new(r"Subscription \{ on_next: 0x[[:xdigit:]]+, on_error: 0x[[:xdigit:]]+, on_complete: 0x[[:xdigit:]]+ \}").unwrap();
        assert!(re.is_match(format!("{:?}", sub).as_str()));
    }
}
