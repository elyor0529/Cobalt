use std::*;

pub use widestring::WideString as String;

#[repr(C)]
#[derive(Ord, PartialOrd, Eq, PartialEq, Hash)]
pub struct Ptr<T>(pub T);

unsafe impl<T> Send for Ptr<T> {}

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

#[repr(C, u64)]
#[derive(Debug)]
pub enum Error {
    Win32(u32),
    Custom(String)
}

#[repr(C, u64)]
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

#[repr(C)]
#[derive(Debug)]
pub struct Subscription<T> {
    pub on_next: Ptr<extern "cdecl" fn(&T)>,
    pub on_error: Ptr<extern "cdecl" fn(&Error)>,
    pub on_complete: Ptr<extern "cdecl" fn(&())>,
}

macro_rules! next {
    ($sub: expr, $e: expr) => {
        $sub.on_next.call($e);
    };
}

macro_rules! err {
    ($sub: expr, $s: expr) => {
        $sub.on_error.call(&ffiext::Error::Custom(ffiext::String::from_str($s)));
    };
}

macro_rules! completed {
    ($sub: expr) => {
        $sub.on_complete.call(&());
    };
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
