mod process;

#[cfg(test)]
mod tests {
    use crate::process::Process;
    use std::time::Duration;
    use ffi_ext::win32::winuser::GetForegroundWindow;

    #[test]
    fn window_extended_info() {
        let proc = Process::start("\"notepad.exe\"");

        let win_e = unsafe { engine::window::window_extended(&proc.main_window().unwrap()) };

        assert_eq!(win_e.process.id, proc.info.dwProcessId);
        assert_eq!(win_e.uwp, ffi_ext::Option::None);
    }

    #[test]
    fn uwp_info() {
        let proc = Process::start("\"calc.exe\"");

        let win_e = unsafe { engine::window::window_extended(&proc.main_window().unwrap()) };

        assert_eq!(win_e.process.id, proc.info.dwProcessId);
        assert_ne!(win_e.uwp, ffi_ext::Option::None);
    }

    #[test]
    fn switch_window() {
        let proc = Process::start("\"notepad.exe\"");
        let proc2 = Process::start("\"C:\\Program Files\\Windows NT\\Accessories\\wordpad.exe\"");

        assert_eq!(proc2.main_window().map(|x| x.hwnd), Some(unsafe { GetForegroundWindow() }));

        proc.switch_to_foreground();

        assert_eq!(proc.main_window().map(|x| x.hwnd), Some(unsafe { GetForegroundWindow() }));
    }
}
