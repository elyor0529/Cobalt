use crate::*;

#[repr(C)]
#[derive(Debug)]
pub struct Subscription<T> {
    pub on_next: ptr::OutFn<T>,
    pub on_error: ptr::OutFn<error::Error>,
    pub on_complete: ptr::OutFn<()>,
}

#[macro_export]
macro_rules! next {
    ($sub: expr, $e: expr) => {
        $sub.on_next.call($e);
    };
}

#[macro_export]
macro_rules! err {
    ($sub: expr, $s: expr) => {
        $sub.on_error.call(&mut $crate::error::Error::Custom(
            $crate::string::String::from_str($s),
        ));
    };
}

#[macro_export]
macro_rules! completed {
    ($sub: expr) => {
        $sub.on_complete.call(&mut ());
    };
}

#[cfg(test)]
mod tests {
    use super::*;

    extern "cdecl" fn sub_on_next(x: &mut u32) {
        println!("what {}", x);
    }

    extern "cdecl" fn sub_on_error(_: &mut error::Error) {}

    extern "cdecl" fn sub_on_compl(_: &mut ()) {}

    pub fn range(start: u32, end: u32, sub: &Subscription<u32>) {
        if end < start {
            err!(sub, "end cannot be before start");
        } else {
            for mut x in start..end {
                next!(sub, &mut x);
            }
        }
        completed!(sub);
    }

    #[test]
    fn subscription_debug() {
        let sub = Subscription {
            on_next: ptr::OutFn(sub_on_next),
            on_error: ptr::OutFn(sub_on_error),
            on_complete: ptr::OutFn(sub_on_compl),
        };
        let re = regex::Regex::new(r"Subscription \{ on_next: 0x[[:xdigit:]]+, on_error: 0x[[:xdigit:]]+, on_complete: 0x[[:xdigit:]]+ \}").unwrap();
        let out = format!("{:?}", sub);
        println!("{}", out);
        let mut inp = 1;
        sub.on_next.call(&mut inp);
        assert!(re.is_match(out.as_str()));
    }

    #[test]
    fn range_test() {
        let sub = Subscription {
            on_next: ptr::OutFn(sub_on_next),
            on_error: ptr::OutFn(sub_on_error),
            on_complete: ptr::OutFn(sub_on_compl),
        };
        range(0, 100, &sub);
    }
}
