pub use winapi::*;
pub use winapi::um::*;
pub use winapi::shared::*;
pub use ntapi::*;

pub mod wintypes {
    pub use winapi::shared::windef::*;
    pub use winapi::shared::minwindef::*;
    pub use winapi::um::winnt::*;
}

#[no_mangle]
pub unsafe fn ticks_to_filetime(ticks: minwindef::DWORD) -> i64 {
    let mut ft: minwindef::FILETIME = std::mem::zeroed();
    sysinfoapi::GetSystemTimePreciseAsFileTime(&mut ft);
    let millis_diff = ticks as i64 - sysinfoapi::GetTickCount64() as i64;
    let ticks = *(&mut ft as *mut _ as *mut i64);
    ticks + millis_diff * 10_000
}


