use proc_macro::TokenStream;
use inflector::Inflector;
use quote::*;

#[proc_macro_attribute]
pub fn watcher_impl(_attr: TokenStream, item: TokenStream) -> TokenStream {
    let impl_: syn::ItemImpl = syn::parse(item).unwrap();

    let &(_, trait_path, _) = &impl_.trait_.as_ref().unwrap();
    let syn::PathSegment { ident: trait_ident, arguments: trait_args } = get_path(trait_path).unwrap();
    let syn::PathSegment { ident: target_ident, .. } = get_type_path(impl_.self_ty.as_ref()).unwrap();
    let generic_param = last_type_param(trait_args);

    let target = target_ident.to_string();
    let target_snake = target.to_snake_case();
    let target_shouty = target.to_screaming_snake_case();


    let fn_begin = syn::Ident::new(format!("{}_begin", target_snake).as_str(), target_ident.span());
    let fn_end = syn::Ident::new(format!("{}_end", target_snake).as_str(), target_ident.span());


    let expanded = match trait_ident.to_string().as_str() {
        "SingletonWatcher" =>  { 
            let instance = syn::Ident::new(format!("{}_INSTANCE", target_shouty).as_str(), target_ident.span());
            let instance_found_err = (quote! { #instance should not be already initialized }).to_string();
            let instance_not_found_err = (quote! { #instance should be already initialized }).to_string();

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
        },
        "TransientWatcher" => {
            let arg_type = type_param(trait_args, 0);

            quote! {
                lazy_static! {
                    static ref WINDOW_CLOSED_GLOBALS: sync::Mutex<std::collections::HashMap<ffi_ext::Ptr<windef::HWND>, WindowClosed>> = {
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
        },
        _ => quote! {}
    };

    dbg!("expanded: \"{}\"", expanded.to_string());
    (quote! { #impl_ #expanded }).into()
}

fn last_type_param(args: &syn::PathArguments) -> Option<&syn::Type> {
    match args {
        syn::PathArguments::AngleBracketed(pargs) => pargs.args.iter().filter_map(
            |arg| if let syn::GenericArgument::Type(typ) = arg { Some(typ) } else { None }).last(),
        _ => None
    }
}

fn type_param(args: &syn::PathArguments, idx: usize) -> Option<&syn::Type> {
    match args {
        syn::PathArguments::AngleBracketed(pargs) => pargs.args.iter().filter_map(
            |arg| if let syn::GenericArgument::Type(typ) = arg { Some(typ) } else { None }).nth(idx),
        _ => None
    }
}

fn get_type_path(typ: &syn::Type) -> Option<&syn::PathSegment> {
    match typ {
        syn::Type::Path(p) => get_path(&p.path),
        _ => None
    }
}

fn get_path(path: &syn::Path) -> Option<&syn::PathSegment> {
    path.segments.last()
}

#[proc_macro]
pub fn instance(item: TokenStream) -> TokenStream {
    let self_ty: syn::Type = syn::parse(item).unwrap();
    let self_ty = match &self_ty {
        syn::Type::Path(p) => &p.path.segments.last().unwrap().ident,
        _ => return TokenStream::new()
    };

    let target_type = self_ty.to_string();
    let target_type_screamingsnake_case = target_type.to_screaming_snake_case();

    let instance = syn::Ident::new(format!("{}_INSTANCE", target_type_screamingsnake_case).as_str(), self_ty.span());
    let instance_not_found_err = (quote! { #instance should be already initialized }).to_string();
    let expanded = quote! { #instance.as_ref().expect(#instance_not_found_err) };

    expanded.into()
}
