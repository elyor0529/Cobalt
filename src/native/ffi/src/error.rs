use crate::*;

#[repr(C, u64)]
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
