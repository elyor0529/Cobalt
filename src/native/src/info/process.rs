use crate::util::*;
use winapi::um::*;
use winapi::shared::*;
use ntapi::*;
use std::*;

#[repr(C)]
pub struct Process {
    id: u32,
    handle: *mut ffi::c_void
}

#[no_mangle]
pub unsafe fn process_for_window(win: windef::HWND) -> Process {
    let mut id = 0;
    let _ = winuser::GetWindowThreadProcessId(win, &mut id);
    let handle = processthreadsapi::OpenProcess(winnt::PROCESS_VM_READ | winnt::PROCESS_QUERY_INFORMATION | winnt::SYNCHRONIZE, 0, id);
    Process { id, handle }
}

#[no_mangle]
pub unsafe fn process_drop(mut proc: mem::ManuallyDrop<Process>) {
    handleapi::CloseHandle(proc.handle);
    mem::ManuallyDrop::drop(&mut proc)
}
