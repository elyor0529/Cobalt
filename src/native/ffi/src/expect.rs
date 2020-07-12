#[macro_export]
macro_rules! expect {
    (true: $e: expr) => {{
        let val = unsafe { $e };
        if val == 0 {
            Err($crate::error::Error::last_win32())
        } else {
            Ok(val)
        }
    }};
    (non_null: $e: expr) => {{
        let val = unsafe { $e };
        if val == std::ptr::null_mut() {
            Err($crate::error::Error::last_win32())
        } else {
            Ok(val)
        }
    }};
}

// TODO 0 is not the only successful error code! refer to the SUCCESS macro
#[macro_export]
macro_rules! hresult {
    ($e: expr) => {{
        let val = unsafe { $e };
        if val < 0 {
            Err($crate::error::Error::HResult(val))
        } else {
            Ok(val)
        }
    }};
}

#[macro_export]
macro_rules! ntstatus {
    ($e: expr) => {{
        let val = unsafe { $e };
        if val < 0 {
            Err($crate::error::Error::NtStatus(val))
        } else {
            Ok(val)
        }
    }};
}
