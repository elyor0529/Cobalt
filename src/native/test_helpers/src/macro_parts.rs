use proc_macros::*;
use quote::*;
use syn::*;

#[cfg(test)]
mod tests {
    use super::*;

    #[ffi_struct]
    pub struct TestStruct(());
    #[ffi_struct(drop)]
    pub struct TestStruct2(());
    type Result<T> = std::result::Result<T, ffi::error::Error>;

    #[ffi_impl]
    impl TestStruct {
        #[ffi_fn]
        pub fn ew(&mut self, lmao: u32) -> Result<u32> {
            Ok(lmao)
        }

        #[ffi_fn]
        pub fn refs(&mut self, mew: &mut u32) -> Result<()> {
            *mew = 4;
            Ok(())
        }

        // NOTE don't elide lifetimes here! Needs to be explicit so that we can capture
        #[ffi_fn]
        pub fn refs2<'a>(&'a mut self, mew: &mut TestStruct) -> Result<&'a mut TestStruct> {
            Ok(self)
        }
    }

    #[test]
    fn test_drop() {
        let mut s = std::mem::ManuallyDrop::new(TestStruct2(()));
        unsafe { test_struct_2_drop(&mut s) };
    }

    #[test]
    fn test_ew() {
        let mut s = TestStruct(());
        let exp = s.ew(3).unwrap();
        let mut o = 0;
        unsafe { test_struct_ew(&mut s, 3, &mut o) };
        assert_eq!(exp, o);
    }

    #[test]
    fn test_refs() {
        let mut s = TestStruct(());
        let mut exp_o = 56;
        let exp = s.refs(&mut exp_o).unwrap();

        let mut o = 0;
        unsafe { test_struct_refs(&mut s, &mut o, &mut ()) };
        assert_eq!(exp_o, o);
    }

    #[test]
    fn test_refs2() {
        let mut s = TestStruct(());
        let mut s2 = TestStruct(());
        let exp = s.refs2(&mut s2).unwrap();

        let mut o = TestStruct(());
        unsafe { test_struct_refs2(&mut s, &mut s2, &mut &mut o) };
    }

    #[test]
    fn function_sig_to_arg() {
        let item = quote!(
            impl TestStruct {
                pub fn nani(lmao: String) -> Result<String> {
                    Result::Ok(lmao)
                }
            }
        );
        let implx: ItemImpl = parse2(item).unwrap();
        let meth1 = implx
            .items
            .iter()
            .filter_map(|x| {
                if let ImplItem::Method(m) = x {
                    Some(m)
                } else {
                    None
                }
            })
            .last()
            .unwrap();

        let fn_ident = &meth1.sig.ident;
        let args: Vec<_> = meth1
            .sig
            .inputs
            .iter()
            .enumerate()
            .map(|(i, _)| format_ident!("arg{}", i))
            .collect();

        let fn_call: Stmt = parse2(quote! {
            #fn_ident(#(#args)*);
        })
        .unwrap();

        dbg!(fn_call.to_token_stream());

        let res: ItemFn = parse2(quote! {
            fn nani(arg0: String) -> Result {
                #fn_call
            }
        })
        .unwrap();
    }
}
