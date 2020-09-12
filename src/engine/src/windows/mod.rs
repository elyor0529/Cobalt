#[macro_use]
pub mod error;
pub mod event_loop;
pub mod hook;

pub use winapi::um::*;
pub use winapi::km::*;
pub use winapi::shared::*;
pub use std::ptr;
pub use std::mem;

pub use crate::windows::error::*;
pub use crate::windows::event_loop::*;
pub use crate::windows::hook::*;

pub mod wintypes {
    pub use winapi::um::winnt::*;
    pub use winapi::shared::windef::*;
    pub use winapi::shared::minwindef::*;
}