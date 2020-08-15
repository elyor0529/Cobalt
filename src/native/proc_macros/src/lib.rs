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
    structx.attrs.push(parse_quote! { #[derive(Debug)] });

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

#[proc_macro_attribute]
pub fn watcher(attr: TokenStream, item: TokenStream) -> TokenStream {
    let args = parse_macro_input!(attr as AttributeArgs);
    let implx = parse_macro_input!(item as ItemImpl);
    let attr = attr0(&args).unwrap();
    watcher_inner(attr, implx)
        .unwrap_or_else(|e| e.to_compile_error().into())
        .into()
}

fn watcher_inner(attr: String, implx: ItemImpl) -> syn::Result<TokenStream> {
    let &(_, trait_path, _) = &implx.trait_.as_ref().unwrap();
    let syn::PathSegment {
        arguments: trait_args,
        ..
    } = get_path(trait_path).unwrap();

    let target = get_type_path(implx.self_ty.as_ref()).unwrap();
    let target_ident = &target.ident;
    let target_str = target.ident.to_string();
    let target_snake = target_str.to_snake_case();
    let target_shouty = target_str.to_screaming_snake_case();

    let t = type_param(trait_args, 0).unwrap();
    let ta = type_param(trait_args, 1).unwrap();

    let new_fn = format_ident!("{}_new", target_snake);
    let drop_fn = format_ident!("{}_new", target_snake);

    let instance = match attr.as_str() {
        "single" => {
            let globals = format_ident!("{}_GLOBAL", target_shouty);

            quote! {

                pub unsafe fn #new_fn<'a>(sub: &'a mut Subscription<#t>, arg: #ta, o: &'a mut #target) -> ffi::Status {
                    match #target_ident::new(sub, arg) {
                        ffi::Result::Ok(x) => {
                            *o = x;
                            ffi::error::Status::Success
                        },
                        ffi::Results::Err(e) => e.into()
                    }
                }

                pub unsafe fn #drop_fn(d: &mut std::mem::ManuallyDrop<#target_ident>) {
                    std::mem::ManuallyDrop::drop(d);
                }

            }
        }
        _ => panic!("Invalid watcher argument"),
    };

    let res = quote! {
        #implx

        #new_fn
        #drop_fn
        #instance
    };
    println!("{}", res.to_string());
    Ok(res.into())
}

#[proc_macro_attribute]
pub fn watcher_impl(_attr: TokenStream, item: TokenStream) -> TokenStream {
    let impl_: syn::ItemImpl = syn::parse(item).unwrap();

    let &(_, trait_path, _) = &impl_.trait_.as_ref().unwrap();
    let syn::PathSegment {
        ident: trait_ident,
        arguments: trait_args,
    } = get_path(trait_path).unwrap();
    let syn::PathSegment {
        ident: target_ident,
        ..
    } = get_type_path(impl_.self_ty.as_ref()).unwrap();
    let generic_param = last_type_param(trait_args);

    let target = target_ident.to_string();
    let target_snake = target.to_snake_case();
    let target_shouty = target.to_screaming_snake_case();

    let fn_begin = syn::Ident::new(
        format!("{}_begin", target_snake).as_str(),
        target_ident.span(),
    );
    let fn_end = syn::Ident::new(
        format!("{}_end", target_snake).as_str(),
        target_ident.span(),
    );

    let expanded = match trait_ident.to_string().as_str() {
        "SingletonWatcher" => {
            let instance = syn::Ident::new(
                format!("{}_INSTANCE", target_shouty).as_str(),
                target_ident.span(),
            );
            let instance_found_err =
                (quote! { #instance should not be already initialized }).to_string();
            let instance_not_found_err =
                (quote! { #instance should be already initialized }).to_string();

            quote! {
                pub static mut #instance: Option<#target_ident> = None;

                #[no_mangle]
                pub unsafe fn #fn_begin(sub: &'static ffi_ext::Subscription<#generic_param>) {
                    #instance.as_ref().expect_none(#instance_found_err);
                    #instance = Some(#target_ident::begin(sub));
                }

                #[no_mangle]
                pub unsafe fn #fn_end() {
                    #instance.take().expect(#instance_not_found_err).end();
                }
            }
        }
        "StatefulWatcher" => {
            quote! {
                #[no_mangle]
                pub unsafe fn #fn_begin(self_v: &'static mut #target_ident<'static>) {
                    self_v.begin();
                }

                #[no_mangle]
                pub unsafe fn #fn_end(obj: #target_ident) {
                    obj.end();
                }
            }
        }
        "TransientWatcher" => {
            let arg_type = type_param(trait_args, 0);
            let globals = syn::Ident::new(
                format!("{}_GLOBALS", target_shouty).as_str(),
                target_ident.span(),
            );

            quote! {

                lazy_static! {
                    static ref #globals: sync::Mutex<std::collections::HashMap<#arg_type, #target_ident<'static>>> = {
                        sync::Mutex::new(std::collections::HashMap::new())
                    };
                }

                #[no_mangle]
                pub unsafe fn #fn_begin(arg: #arg_type, sub: &ffi_ext::Subscription<#generic_param>) -> #target_ident {
                    #target_ident::begin(arg, sub)
                }

                #[no_mangle]
                pub unsafe fn #fn_end(obj: #target_ident) {
                    obj.end();
                }
            }
        }
        _ => quote! {},
    };

    // dbg!("expanded: \"{}\"", expanded.to_string());
    (quote! { #impl_ #expanded }).into()
}

fn last_type_param(args: &syn::PathArguments) -> Option<&syn::Type> {
    match args {
        syn::PathArguments::AngleBracketed(pargs) => pargs
            .args
            .iter()
            .filter_map(|arg| {
                if let syn::GenericArgument::Type(typ) = arg {
                    Some(typ)
                } else {
                    None
                }
            })
            .last(),
        _ => None,
    }
}

fn type_param(args: &syn::PathArguments, idx: usize) -> Option<&syn::Type> {
    match args {
        syn::PathArguments::AngleBracketed(pargs) => pargs
            .args
            .iter()
            .filter_map(|arg| {
                if let syn::GenericArgument::Type(typ) = arg {
                    Some(typ)
                } else {
                    None
                }
            })
            .nth(idx),
        _ => None,
    }
}

fn get_type_path(typ: &syn::Type) -> Option<&syn::PathSegment> {
    match typ {
        syn::Type::Path(p) => get_path(&p.path),
        _ => None,
    }
}

fn get_path(path: &syn::Path) -> Option<&syn::PathSegment> {
    path.segments.last()
}

#[proc_macro]
pub fn singleton_instance(item: TokenStream) -> TokenStream {
    let self_ty: syn::Type = syn::parse(item).unwrap();
    let self_ty = match &self_ty {
        syn::Type::Path(p) => &p.path.segments.last().unwrap().ident,
        _ => return TokenStream::new(),
    };

    let target_type = self_ty.to_string();
    let target_type_screamingsnake_case = target_type.to_screaming_snake_case();

    let instance = syn::Ident::new(
        format!("{}_INSTANCE", target_type_screamingsnake_case).as_str(),
        self_ty.span(),
    );
    let instance_not_found_err = (quote! { #instance should be already initialized }).to_string();
    let expanded = quote! { #instance.as_ref().expect(#instance_not_found_err) };

    expanded.into()
}

#[proc_macro]
pub fn transient_globals(item: TokenStream) -> TokenStream {
    let self_ty: syn::Type = syn::parse(item).unwrap();
    let self_ty = match &self_ty {
        syn::Type::Path(p) => &p.path.segments.last().unwrap().ident,
        _ => return TokenStream::new(),
    };

    let target_type = self_ty.to_string();
    let target_shouty = target_type.to_screaming_snake_case();
    let globals = syn::Ident::new(
        format!("{}_GLOBALS", target_shouty).as_str(),
        self_ty.span(),
    );

    let expanded = quote! {
        #globals.lock().unwrap()
    };

    expanded.into()
}
