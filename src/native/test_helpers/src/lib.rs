#![feature(maybe_uninit_ref)]
#![feature(maybe_uninit_extra)]

mod event_loop;
mod macro_parts;
mod process;

#[cfg(test)]
mod tests {
    use crate::event_loop::EventLoop;
    use crate::process::Process;
    use engine::window::foreground::*;
    use ffi::{ManuallyDrop, Out};
    use std::sync::{Arc, Mutex};
    use std::time::Duration;

    #[test]
    fn window_extended_info() {
        let proc = Process::start("\"notepad.exe\"");
        let mut win_e = proc.main_window().unwrap();
        win_e.extended().unwrap();

        assert_eq!(
            win_e.extended.as_ref().unwrap().process.id,
            proc.info.dwProcessId
        );
        assert_eq!(win_e.extended.as_ref().unwrap().uwp, ffi::Option::None);
    }

    #[test]
    fn uwp_info() {
        let proc = Process::start("\"calc.exe\"");
        let mut win_e = proc.main_window().unwrap();
        win_e.extended().unwrap();

        assert_eq!(
            win_e.extended.as_ref().unwrap().process.id,
            proc.info.dwProcessId
        );
        assert_eq!(win_e.extended.as_ref().unwrap().uwp, ffi::Option::None);
    }

    #[test]
    fn winrt_import() {
        //let what = ffi::win32::AppInfo = unsafe { std::mem::zeroed() };
        let test_aumid = "Microsoft.SkypeApp_kzf8qxf38zg5c!App";
        let result =
            ffi::windows::system::AppDiagnosticInfo::request_info_for_app_user_model_id(test_aumid)
                .expect("async operation to be returned")
                .get()
                .expect("async operation to be completed");
        for x in 0..result.size().unwrap() {
            let app_diagnostic_info = result.get_at(x).expect("item at index");
            let app_info = app_diagnostic_info.app_info().unwrap();
            let display_info = app_info.display_info().unwrap();
            dbg!(app_info.app_user_model_id().unwrap());
            dbg!(display_info.description().unwrap());
            dbg!(display_info.display_name().unwrap());
            let logo = display_info
                .get_logo(ffi::windows::foundation::Size {
                    width: 144.0,
                    height: 144.0,
                })
                .unwrap()
                .open_read_async()
                .unwrap()
                .get() // get to get async result
                .unwrap();
            let logo_sz = logo.size().unwrap();
            let logo_reader =
                ffi::windows::storage::streams::DataReader::create_data_reader(logo).unwrap();
            logo_reader
                .load_async(logo_sz as u32)
                .unwrap()
                .get()
                .unwrap();
            let mut buffer = vec![0u8; logo_sz as usize];
            logo_reader.read_bytes(&mut buffer).unwrap();

            assert_eq!(buffer.len(), logo_sz as usize);
            dbg!(&buffer[0..10]);
        }
        dbg!(result);
    }

    #[test]
    fn switch_window() {
        let proc = Process::start("\"notepad.exe\"");
        let proc2 = Process::start("\"C:\\Program Files\\Windows NT\\Accessories\\wordpad.exe\"");

        assert_eq!(
            proc2.main_window().map(|x| x.basic.hwnd),
            Some(unsafe { ffi::windows::winuser::GetForegroundWindow() })
        );

        proc.switch_to_foreground();

        assert_eq!(
            proc.main_window().map(|x| x.basic.hwnd),
            Some(unsafe { ffi::windows::winuser::GetForegroundWindow() })
        );
    }

    static mut VALUES: Vec<ForegroundWindowSwitch> = Vec::new();
    static mut ERRORS: Vec<ffi::Error> = Vec::new();
    static mut COMPLETED_CALLED: u32 = 0;

    extern "cdecl" fn sub_on_next(x: &mut ForegroundWindowSwitch) {
        unsafe { VALUES.push(std::ptr::read(x as *mut _)) }
    }

    extern "cdecl" fn sub_on_error(e: &mut ffi::Error) {
        unsafe { ERRORS.push(std::ptr::read(e as *mut _)) }
    }

    extern "cdecl" fn sub_on_compl(_: &mut ()) {
        unsafe {
            COMPLETED_CALLED += 1;
        }
    }

    static mut SUB: ffi::Subscription<ForegroundWindowSwitch> = ffi::Subscription {
        on_next: ffi::ptr::OutFn(sub_on_next),
        on_error: ffi::ptr::OutFn(sub_on_error),
        on_complete: ffi::ptr::OutFn(sub_on_compl),
    };

    static mut WATCHER: std::mem::MaybeUninit<ForegroundWindowWatcher> =
        std::mem::MaybeUninit::uninit();

    #[test]
    fn test_fg_watcher() {
        let proc = Process::start("\"notepad.exe\"");
        let proc2 = Process::start("\"C:\\Program Files\\Windows NT\\Accessories\\wordpad.exe\"");
        let ev = Arc::new(Mutex::new(EventLoop::new()));

        let ev2 = ev.clone();
        let wa = std::thread::spawn(move || {
            assert_eq!(
                unsafe { foreground_window_watcher_begin(&mut WATCHER, &mut SUB) },
                ffi::Status::Success
            );

            assert_eq!(EventLoop::run(ev2), None);

            unsafe { foreground_window_watcher_end(&mut ManuallyDrop::new(WATCHER.read())) }
            std::thread::sleep(Duration::new(1, 0));

            unsafe {
                assert_eq!(COMPLETED_CALLED, 1);
                assert!(ERRORS.is_empty());
                assert_eq!(VALUES.len(), 2);
            }
        });

        proc.switch_to_foreground();
        std::thread::sleep(Duration::new(1, 0));

        unsafe {
            assert_eq!(COMPLETED_CALLED, 0);
            assert!(ERRORS.is_empty());
            assert_eq!(VALUES.len(), 1);
            assert_eq!(VALUES[0].window.basic, proc.main_window().unwrap().basic);
        }

        proc2.switch_to_foreground();
        std::thread::sleep(Duration::new(1, 0));

        unsafe {
            assert_eq!(COMPLETED_CALLED, 0);
            assert!(ERRORS.is_empty());
            assert_eq!(VALUES.len(), 2);
            assert_eq!(VALUES[1].window.basic, proc2.main_window().unwrap().basic);
        }

        loop {
            match ev.lock() {
                Ok(mut x) => {
                    x.cancel();
                    break;
                }
                Err(e) => (),
            }
        }
        wa.join().expect("cannot join??")
    }
}
