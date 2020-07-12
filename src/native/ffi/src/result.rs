use std::ops::Try;

#[repr(C, u64)]
#[derive(Debug, Ord, PartialOrd, Eq, PartialEq)]
pub enum Result<T> {
    Ok(T),
    Err(crate::error::Error),
}

impl<T> Result<T> {
    pub fn unwrap(self) -> T {
        if let Result::Ok(x) = self {
            x
        } else {
            panic!("unwrap failed");
        }
    }
}

impl<T> Try for Result<T> {
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
            Err(e) => Result::Err(e.into()),
        }
    }
}

impl From<std::boxed::Box<dyn std::error::Error>> for crate::error::Error {
    fn from(e: std::boxed::Box<dyn std::error::Error>) -> Self {
        crate::error::Error::Custom(e.to_string().into())
    }
}
