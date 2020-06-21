use crate::*;
use std::*;

#[repr(C)]
pub struct Basic {
    id: u32,
    title: ffiext::String
}

#[repr(C)]
pub struct Extended {
    uwp_aumid: ffiext::Nullable<ffiext::String>
}
