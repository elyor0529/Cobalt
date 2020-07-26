pub type Result<T> = std::result::Result<T, crate::error::Error>;

impl From<std::boxed::Box<dyn std::error::Error>> for crate::error::Error {
    fn from(e: std::boxed::Box<dyn std::error::Error>) -> Self {
        crate::error::Error::Custom(e.to_string().into())
    }
}
