use crate::windows::*;

pub struct Ticks(pub minwindef::DWORD);

impl Ticks {
    pub fn filetime(self) -> i64 {
        unsafe {
            let ticks = self.0;
            let mut ft: minwindef::FILETIME = Default::default();
            sysinfoapi::GetSystemTimePreciseAsFileTime(&mut ft);
            let millis_diff = ticks as i64 - sysinfoapi::GetTickCount64() as i64;
            let ticks = *(&mut ft as *mut _ as *mut i64);
            ticks + millis_diff * 10_000
        }
    }
}
