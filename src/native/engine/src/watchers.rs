pub trait SingletonWatcher<'a, T> {
    fn begin(sub: &'a ffi_ext::Subscription<T>) -> Self;
    fn end(self); 
}

pub trait TransientWatcher<'a, TA, TR> {
    fn begin(arg: TA, sub: &'a ffi_ext::Subscription<TR>) -> Self;
    fn end(self);
}
