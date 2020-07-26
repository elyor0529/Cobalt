#[repr(C, u64)]
#[derive(Debug, Ord, PartialOrd, Eq, PartialEq, Hash)]
pub enum Option<T> {
    Some(T),
    None,
}

impl<T> Option<T> {
    pub fn unwrap(self) -> T {
        if let Option::Some(x) = self {
            x
        } else {
            panic!("Option is None")
        }
    }

    pub fn as_ref(&self) -> Option<&T> {
        if let Option::Some(x) = self {
            Option::Some(x)
        } else {
            Option::None
        }
    }
}

impl<T> From<std::option::Option<T>> for Option<T> {
    fn from(opt: std::option::Option<T>) -> Self {
        match opt {
            Some(x) => Option::Some(x),
            None => Option::None,
        }
    }
}
