use ffi_ext::win32::*;
use winapi::_core::time::Duration;
use std::io::Error;

pub struct Process {
    pub info: processthreadsapi::PROCESS_INFORMATION
}

impl Process {
    pub fn start(cmd_line: &str) -> Process {
        let cmd_line = ffi_ext::String::from_str(cmd_line);
        let mut si: processthreadsapi::STARTUPINFOW = unsafe { std::mem::zeroed() };
        si.cb = std::mem::size_of::<processthreadsapi::STARTUPINFOW>() as u32;
        let mut pi: processthreadsapi::PROCESS_INFORMATION = unsafe { std::mem::zeroed() };
        let proc = unsafe {
            if processthreadsapi::CreateProcessW(
                std::ptr::null_mut(),
                cmd_line.into_vec().as_mut_ptr(),
                std::ptr::null_mut(),
                std::ptr::null_mut(),
                1,
                0,
                std::ptr::null_mut(),
                std::ptr::null_mut(),
                &mut si as *mut _ as *mut processthreadsapi::STARTUPINFOW,
                &mut pi as *mut _ as *mut processthreadsapi::PROCESS_INFORMATION
            ) == 0 {
                panic!(Error::last_os_error().to_string());
            }
            let proc = Process { info: pi };

            //winuser::WaitForInputIdle(pi.hProcess, winbase::INFINITE);
            loop {
                if let Some(win) = proc.main_window() {
                    if win.title.len() != 0 {
                        break
                    }
                }
                std::thread::sleep(Duration::from_millis(1000))
            }
            proc
        };
        proc
    }

    pub fn switch_to_foreground(&self) {
        let mut key_state = vec![0u8; 256];
        unsafe {
            if winuser::GetKeyboardState(key_state.as_mut_ptr()) != 0 && key_state[winuser::VK_MENU as usize] & 0x80 == 0 {
                winuser::keybd_event(winuser::VK_MENU as u8, 0, winuser::KEYEVENTF_EXTENDEDKEY, 0);
            }

            if let Some(win) = self.main_window() {
                winuser::SetForegroundWindow(win.hwnd);
            }
            else {
                println!("switch failed");
            }

            if winuser::GetKeyboardState(key_state.as_mut_ptr()) != 0 && key_state[winuser::VK_MENU as usize] & 0x80 == 0 {
                winuser::keybd_event(winuser::VK_MENU as u8, 0, winuser::KEYEVENTF_EXTENDEDKEY | winuser::KEYEVENTF_KEYUP, 0);
            }
        };
    }

    pub fn main_window(&self) -> Option<engine::window::Basic> {
        let mut dat: (u32, Option<wintypes::HWND>) = (self.info.dwProcessId, None);
        unsafe { winuser::EnumWindows(Some(Process::enum_windows_callback), &mut dat as *mut (u32, Option<wintypes::HWND>) as isize) };
        dat.1.map(|hwnd| engine::window::Basic { hwnd, title: unsafe { engine::window::title(hwnd) } } )
    }

    extern "system" fn enum_windows_callback(hwnd: wintypes::HWND, lparam: isize) -> i32 {
        let dat = unsafe { &mut *(lparam as *mut (u32, Option<wintypes::HWND>)) };
        if dat.0 == engine::window::pid_tid(hwnd).0 && Process::is_main_window(hwnd) {
            dat.1 = Some(hwnd);
            0
        }
        else {
            1
        }
    }

    fn is_main_window(handle: wintypes::HWND) -> bool {
        unsafe { winuser::GetWindow(handle, winuser::GW_OWNER) == std::ptr::null_mut() && winuser::IsWindowVisible(handle) != 0 }
    }
}

impl Drop for Process {
    fn drop(&mut self) {
        unsafe { processthreadsapi::TerminateProcess(self.info.hProcess, 0) };
    }
}