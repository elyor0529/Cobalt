use winapi::_core::ops::Try;

#[repr(C, u64)]
#[derive(Debug, Ord, PartialOrd, Eq, PartialEq)]
pub enum Result<T> {
    Ok(T),
    Err(crate::error::Error),
}

impl<T> std::ops::Try for Result<T> {
    type Ok = T;
    type Error = crate::error::Error;

    fn into_result(self) -> std::result::Result<<Result<T> as Try>::Ok, Self::Error> {
        match self {
            Result::Ok(x) => Ok(x),
            Result::Err(e) => Err(e),
        }
    }

    fn from_error(v: Self::Error) -> Self {
        Result::Err(v)
    }

    fn from_ok(v: <Result<T> as Try>::Ok) -> Self {
        Result::Ok(v)
    }
}

impl<T> From<std::result::Result<T, std::boxed::Box<dyn std::error::Error>>> for Result<T> {
    fn from(res: std::result::Result<T, std::boxed::Box<dyn std::error::Error>>) -> Self {
        match res {
            Ok(x) => Result::Ok(x),
            Err(e) => Result::Err(crate::error::Error::Custom(e.to_string().into())),
        }
    }
}
