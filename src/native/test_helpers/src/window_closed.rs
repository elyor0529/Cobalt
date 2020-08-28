#[cfg(test)]
mod tests {
    use crate::event_loop::EventLoop;
    use crate::process::Process;
    use engine::window::closed::*;
    use ffi::*;
    use std::sync::{Arc, Mutex};
    use std::time::Duration;

    static mut VALUES: Vec<()> = Vec::new();
    static mut ERRORS: Vec<ffi::Error> = Vec::new();
    static mut COMPLETED_CALLED: u32 = 0;

    extern "cdecl" fn sub_on_next(x: &mut ()) {
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

    static mut SUB: ffi::Subscription<()> = ffi::Subscription {
        on_next: ffi::ptr::OutFn(sub_on_next),
        on_error: ffi::ptr::OutFn(sub_on_error),
        on_complete: ffi::ptr::OutFn(sub_on_compl),
    };

    static mut WATCHER: std::mem::MaybeUninit<WindowClosed> = std::mem::MaybeUninit::uninit();

    #[test]
    fn test_close() {
        let proc = Process::start("\"notepad.exe\"");
        //let proc2 = Process::start("\"C:\\Program Files\\Windows NT\\Accessories\\wordpad.exe\"");
        let ev = Arc::new(Mutex::new(EventLoop::new()));

        std::thread::sleep(Duration::new(1, 0));
        let procm = proc.main_window().unwrap();
        //let proc2m = proc2.main_window().unwrap();
        let hwnd1 = procm.basic.hwnd as usize;
        //let hwnd2 = procm.basic.hwnd;

        let ev2 = ev.clone();
        let wa = std::thread::spawn(move || {
            assert_eq!(
                unsafe {
                    window_closed_watcher_begin(
                        &mut WATCHER,
                        WinHandle(hwnd1 as ffi::windows::wintypes::HWND),
                        &mut SUB,
                    )
                },
                ffi::Status::Success
            );

            assert_eq!(EventLoop::run(ev2), None);

            // unsafe { window_closed_watcher_end(&mut ManuallyDrop::new(WATCHER.read())) }
            // std::thread::sleep(Duration::new(1, 0));

            //unsafe {
            //    assert_eq!(COMPLETED_CALLED, 1);
            //    assert!(ERRORS.is_empty());
            //    assert_eq!(VALUES.len(), 2);
            //}
        });

        drop(proc);
        std::thread::sleep(Duration::new(5, 0));

        unsafe {
            assert!(ERRORS.is_empty());
            assert_eq!(VALUES.len(), 1);
            assert_eq!(COMPLETED_CALLED, 1);
        }

        /*
        drop(proc2);
        std::thread::sleep(Duration::new(1, 0));

        unsafe {
            assert_eq!(COMPLETED_CALLED, 2);
            assert!(ERRORS.is_empty());
            assert_eq!(VALUES.len(), 2);
        }
        */

        loop {
            match ev.lock() {
                Ok(mut x) => {
                    x.cancel();
                    break;
                }
                Err(_) => (),
            }
        }
        wa.join().expect("cannot join??")
    }
}
