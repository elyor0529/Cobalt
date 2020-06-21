use std::*;

#[repr(C)]
pub struct Basic {
    id: u32,
    title: ffi_ext::String
}

#[repr(C)]
pub struct Extended {
    uwp_aumid: ffi_ext::Nullable<ffi_ext::String>
}
