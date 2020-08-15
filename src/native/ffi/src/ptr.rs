use std::fmt::*;

pub use std::mem::*;
pub use std::ptr::*;

#[repr(C)]
pub struct OutFn<T>(pub extern "cdecl" fn(&mut T));

unsafe impl<T> Send for OutFn<T> {}
unsafe impl<T> Sync for OutFn<T> {}

impl<T> OutFn<T> {
    #[inline(always)]
    pub fn call(&self, v: &mut T) {
        (&self.0)(v)
    }
}

impl<T> Debug for OutFn<T> {
    fn fmt(&self, f: &mut Formatter<'_>) -> Result {
        write!(f, "{:p}", &self.0)
    }
}

#[repr(C)]
pub struct Out<T>(*mut T);

// you can only set Out<T> once!
impl<T> Out<T> {
    #[inline(always)]
    pub fn set(self, val: T) {
        unsafe {
            *self.0 = val;
        }
    }
}

#[repr(C)]
#[derive(Debug, Ord, PartialOrd, Eq, PartialEq)]
pub struct Box<T>(*mut T);

impl<T> Box<T> {
    pub fn new(val: T) -> Self {
        Box(std::boxed::Box::leak(std::boxed::Box::new(val)))
    }
}

impl<T> Drop for Box<T> {
    fn drop(&mut self) {
        // gets the box and drops it
        unsafe { std::boxed::Box::from_raw(self.0) };
    }
}

impl<T> std::ops::Deref for Box<T> {
    type Target = T;

    fn deref(&self) -> &Self::Target {
        unsafe { &*self.0 }
    }
}

impl<T> std::ops::DerefMut for Box<T> {
    fn deref_mut(&mut self) -> &mut Self::Target {
        unsafe { &mut *self.0 }
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    extern "cdecl" fn test_fn(x: &mut u32) {
        *x = 32;
    }

    #[test]
    fn fnptr_debug() {
        let fn_ptr = OutFn(test_fn);
        let re = regex::Regex::new(r"0x[[:xdigit:]]+").unwrap();
        let out = format!("{:?}", fn_ptr);
        assert!(re.is_match(out.as_str()));
    }

    #[test]
    fn fnptr_mut_call() {
        let fn_ptr = OutFn(test_fn);
        let mut inp = 3;
        fn_ptr.call(&mut inp);
        assert_eq!(inp, 32);
    }
}

/*
impl<T> fmt::Debug for Ptr<*mut T> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{:p}", &self.0)
    }
}
impl<T> fmt::Debug for Ptr<*const T> {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        write!(f, "{:p}", &self.0)
    }
}*/
