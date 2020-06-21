use proc_macro::TokenStream;

#[proc_macro_attribute]
pub fn watcher(attr: TokenStream, item: TokenStream) -> TokenStream {
    println!("attr: \"{}\"", attr.to_string());
    println!("item: \"{}\"", item.to_string());
    let imp: syn::ItemImpl = syn::parse(item).unwrap();

    let &(_, trait_, _) = &imp.trait_.as_ref().unwrap();
    let self_ty = match &(*imp.self_ty) {
        syn::Type::Path(p) => &p.path.segments.last().unwrap().ident,
        _ => return attr
    };

    let snake_case_type = quote::format_ident!("{}_begin", casey::snake!(self_ty));

    let expanded = quote::quote! {
        #imp
        #snake_case_type
    };

    println!("expanded: \"{}\"", expanded.to_string());
    expanded.into()
}
