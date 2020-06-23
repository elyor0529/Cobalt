use proc_macro::TokenStream;
use inflector::Inflector;
use quote::*;

#[proc_macro_attribute]
pub fn watcher_impl(attr: TokenStream, item: TokenStream) -> TokenStream {
    println!("attr: \"{}\"", attr.to_string());
    println!("item: \"{}\"", item.to_string());
    let imp: syn::ItemImpl = syn::parse(item).unwrap();

    let &(_, trait_path, _) = &imp.trait_.as_ref().unwrap();
    let self_ty = get_type_ident(imp.self_ty.as_ref()).unwrap();

    let target_type = self_ty.to_string();
    let target_type_snake_case = target_type.to_snake_case();
    let target_type_screamingsnake_case = target_type.to_screaming_snake_case();


    let fn_begin = syn::Ident::new(format!("{}_begin", target_type_snake_case).as_str(), self_ty.span());
    let fn_end = syn::Ident::new(format!("{}_end", target_type_snake_case).as_str(), self_ty.span());


    let expanded = match get_path_ident(trait_path).unwrap().to_string().as_str() {
        "SingleInstanceWatcher" =>  { 
            let instance = syn::Ident::new(format!("{}_INSTANCE", target_type_screamingsnake_case).as_str(), self_ty.span());

            quote! {
                pub static mut #instance: Option<#self_ty> = None;

                #[no_mangle]
                pub unsafe fn #fn_begin(sub: &'static ffi_ext::Subscription<u32>) {
                    #instance = Some(#self_ty::begin(sub));
                }

                #[no_mangle]
                pub unsafe fn #fn_end() {
                    #instance.take().unwrap().end();
                }
            }
        },
        _ => quote! {}
    };

    println!("expanded: \"{}\"", expanded.to_string());
    (quote! { #imp #expanded }).into()
}

fn get_type_ident(typ: &syn::Type) -> Option<&syn::Ident> {
    match typ {
        syn::Type::Path(p) => get_path_ident(&p.path),
        _ => None
    }
}

fn get_path_ident(path: &syn::Path) -> Option<&syn::Ident> {
    path.segments.last().map(|x| &x.ident)
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
