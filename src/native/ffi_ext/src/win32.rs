pub use winapi::*;
pub use winapi::um::*;
pub use winapi::shared::*;
pub use ntapi::*;

pub mod wintypes {
    pub use winapi::ctypes::*;
    pub use winapi::shared::windef::*;
    pub use winapi::shared::minwindef::*;
    pub use winapi::um::winnt::*;
}

pub struct Ticks(pub minwindef::DWORD);

impl Ticks {
    pub fn as_filetime(self) -> i64 {
        unsafe {
            let ticks = self.0;
            let mut ft: minwindef::FILETIME = std::mem::zeroed();
            sysinfoapi::GetSystemTimePreciseAsFileTime(&mut ft);
            let millis_diff = ticks as i64 - sysinfoapi::GetTickCount64() as i64;
            let ticks = *(&mut ft as *mut _ as *mut i64);
            ticks + millis_diff * 10_000
        }
    }
}
