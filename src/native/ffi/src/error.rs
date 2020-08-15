use crate::*;

#[derive(Ord, PartialOrd, Eq, PartialEq, Debug)]
pub enum Error {
    Win32(i32),
    HResult(i32),
    NtStatus(i32),
    Custom(string::String),
}

impl Error {
    pub fn last_win32() -> Self {
        Error::Win32(unsafe { crate::windows::errhandlingapi::GetLastError() as i32 })
    }
}

#[repr(C, u64)]
#[derive(Ord, PartialOrd, Eq, PartialEq, Debug)]
pub enum Status {
    Win32(i32),
    HResult(i32),
    NtStatus(i32),
    Custom(Box<string::String>),
    Success,
}

impl From<Error> for Status {
    fn from(e: Error) -> Self {
        match e {
            Error::Win32(x) => Status::Win32(x),
            Error::HResult(x) => Status::HResult(x),
            Error::NtStatus(x) => Status::NtStatus(x),
            Error::Custom(x) => Status::Custom(Box::new(x)),
        }
    }
}

#[no_mangle] // invoke this only when `s` is `Status::Custom`
pub unsafe fn status_drop(s: &mut std::mem::ManuallyDrop<Status>) {
    std::mem::ManuallyDrop::drop(s);
}
