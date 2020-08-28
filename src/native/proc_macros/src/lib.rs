use inflector::Inflector;
use proc_macro::TokenStream;
use proc_macro2;
use quote::*;
use syn::*;

#[proc_macro_attribute]
pub fn ffi_impl(attr: TokenStream, item: TokenStream) -> TokenStream {
    let args = parse_macro_input!(attr as AttributeArgs);
    let implx = parse_macro_input!(item as ItemImpl);
    ffi_ify_inner(args, implx)
        .unwrap_or_else(|e| e.to_compile_error().into())
        .into()
}

#[proc_macro_attribute] // marker attribute
pub fn ffi_struct(attr: TokenStream, item: TokenStream) -> TokenStream {
    let args = parse_macro_input!(attr as AttributeArgs);
    let mut structx = parse_macro_input!(item as ItemStruct);

    structx.attrs.push(parse_quote! { #[repr(C)] });
    structx
        .attrs
        .push(parse_quote! { #[derive(Debug, Eq, PartialEq, Ord, PartialOrd)] });

    let self_ty = &structx.ident;
    let self_ty_name = quote!( #self_ty ).to_string();
    let self_ty_drop = format_ident!("{}_drop", self_ty_name.to_snake_case());

    let gen_drop_fn = attr0(&args) == Some("drop".to_string());

    let drop_fn = if gen_drop_fn {
        quote! {
            #[no_mangle]
            pub unsafe fn #self_ty_drop(x: &mut std::mem::ManuallyDrop<#self_ty>) {
                std::mem::ManuallyDrop::drop(x);
            }
        }
    } else {
        quote!()
    };

    (quote! {
        #structx

        #drop_fn
    })
    .into()
}

#[proc_macro_attribute] // marker attribute
pub fn ffi_enum(_: TokenStream, item: TokenStream) -> TokenStream {
    let mut enumx = parse_macro_input!(item as ItemEnum);

    enumx.attrs.push(parse_quote! { #[derive(Debug)] });

    let drop_fn = type_drop_fn(&enumx.ident);

    (quote! {
        #enumx

        #drop_fn
    })
    .into()
}

#[proc_macro_attribute] // marker attribute
pub fn ffi_fn(_: TokenStream, fun: TokenStream) -> TokenStream {
    fun
}

fn ffi_ify_inner(_: AttributeArgs, implx: ItemImpl) -> syn::Result<TokenStream> {
    let implx2 = implx.clone();

    let fnxs: Vec<_> = implx2
        .items
        .iter()
        .filter_map(|x| {
            if let ImplItem::Method(m) = x {
                if is_ffi_fn(&m) {
                    return Some(m);
                }
            }
            None
        })
        .filter(|m| is_self_fn(&m.sig))
        .map(|x| self_result_fn_to_ffi_fn(x.sig.clone(), implx.self_ty.as_ref()).unwrap())
        .collect();

    let output = quote! {
        #implx

        #(#fnxs)*
    };

    Ok(output.into())
}

fn attr0(args: &AttributeArgs) -> Option<String> {
    for a in args {
        if let NestedMeta::Meta(m) = a {
            if let Meta::Path(p) = m {
                return Some(p.segments[0].ident.to_string());
            }
        }
    }
    None
}

fn is_self_fn(sig: &Signature) -> bool {
    sig.receiver().is_some()
}

fn is_ffi_fn(m: &ImplItemMethod) -> bool {
    m.attrs
        .iter()
        .any(|a| a.path.get_ident().unwrap().to_token_stream().to_string() == "ffi_fn")
}

fn result_fn_arg(sig: &Signature) -> Option<&Type> {
    if let ReturnType::Type(_, ret) = &sig.output {
        if let Type::Path(path) = &**ret {
            let last_seg = path.path.segments.last().unwrap();
            if last_seg.ident.to_string() == "Result" {
                if let PathArguments::AngleBracketed(ang) = &last_seg.arguments {
                    if let Some(GenericArgument::Type(res_ok_ty)) = ang.args.first() {
                        return Some(res_ok_ty);
                    }
                }
            }
        }
    }
    None
}

fn self_result_fn_to_ffi_fn(sig: Signature, self_ty: &Type) -> Result<ItemFn> {
    let self_ty_name = quote! { #self_ty }.to_string();
    let self_ty_lf = if let Some(FnArg::Receiver(r)) = sig.inputs.first() {
        r.lifetime()
    } else {
        None
    };
    let fn_name = sig.ident.clone();
    let arg = result_fn_arg(&sig).unwrap();
    let mut output_sig = sig.clone();

    output_sig.ident = format_ident!("{}_{}", self_ty_name.to_snake_case(), sig.ident);
    *output_sig.inputs.first_mut().unwrap() = parse_quote! { x: &#self_ty_lf mut #self_ty };
    output_sig.output = parse_quote! { -> ffi::error::Status };

    let mut num_args: usize = 0;
    for (i, x) in output_sig.inputs.iter_mut().enumerate() {
        if let FnArg::Typed(t) = x {
            let ident = format_ident!("arg{}", i);
            t.pat = parse_quote!(#ident);
            num_args += 1;
        }
    }
    let args: Vec<_> = (1..num_args).map(|x| format_ident!("arg{}", x)).collect();

    output_sig.inputs.push(parse2(quote! { o: &mut #arg })?);
    output_sig.unsafety = Some(Default::default());

    let res_fn: ItemFn = parse_quote! {
        #[no_mangle]
        #output_sig {
            ffi::write_out(arg0.#fn_name(#(#args),*), o)
        }
    };
    Ok(res_fn)
}

fn type_drop_fn(t: &Ident) -> proc_macro2::TokenStream {
    let fn_name = format_ident!("{}_drop", t.to_token_stream().to_string());
    quote! {
        unsafe fn #fn_name(o: &mut std::mem::ManuallyDrop<#t>) {
            std::mem::ManuallyDrop::drop(o);
        }
    }
}
