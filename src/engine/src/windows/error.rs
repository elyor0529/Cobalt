use std::fmt::*;

#[derive(Ord, PartialOrd, Eq, PartialEq, Debug)]
pub enum WinError {
    Win32(i32),
    HResult(i32),
    NtStatus(i32),
}

impl WinError {
    pub fn last_win32() -> Self {
        WinError::Win32(unsafe { crate::windows::errhandlingapi::GetLastError() as i32 })
    }
}

impl std::fmt::Display for WinError {
    fn fmt(&self, fmt: &mut std::fmt::Formatter<'_>) -> std::result::Result<(), std::fmt::Error> {
        match self {
            Self::Win32(r) => write!(fmt, "Win32 ({})", r),
            Self::HResult(r) => write!(fmt, "HResult ({})", r),
            Self::NtStatus(r) => write!(fmt, "NtStatus ({})", r),
        }
    }
}

impl std::error::Error for WinError {}

macro_rules! expect {
    (true: $e: expr) => {{
        let val = unsafe { $e };
        if val == 0 {
            Err($crate::windows::WinError::last_win32())
        } else {
            Ok(val)
        }
    }};
    (non_null: $e: expr) => {{
        let val = unsafe { $e };
        if val == std::ptr::null_mut() {
            Err($crate::windows::WinError::last_win32())
        } else {
            Ok(val)
        }
    }};
}

// TODO 0 is not the only successful error code! refer to the SUCCESS macro
macro_rules! hresult {
    ($e: expr) => {{
        let val = unsafe { $e };
        if val < 0 {
            Err($crate::windows::WinError::HResult(val))
        } else {
            Ok(val)
        }
    }};
}

macro_rules! ntstatus {
    ($e: expr) => {{
        let val = unsafe { $e };
        if val < 0 {
            Err($crate::windows::WinError::NtStatus(val))
        } else {
            Ok(val)
        }
    }};
}
