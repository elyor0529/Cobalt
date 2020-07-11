#[repr(C, u64)]
#[derive(Debug, Ord, PartialOrd, Eq, PartialEq, Hash)]
pub enum Option<T> {
    Some(T),
    None,
}

impl<T> From<std::option::Option<T>> for Option<T> {
    fn from(opt: std::option::Option<T>) -> Self {
        match opt {
            Some(x) => Option::Some(x),
            None => Option::None,
        }
    }
}
