#![feature(maybe_uninit_ref)]

mod process;

#[cfg(test)]
mod tests {
    use crate::process::Process;

    #[test]
    fn window_extended_info() {
        let proc = Process::start("\"notepad.exe\"");
        let mut win_e = proc.main_window().unwrap();
        win_e.extended().unwrap();

        assert_eq!(
            unsafe { win_e.extended.get_ref() }.process.id,
            proc.info.dwProcessId
        );
        assert_eq!(unsafe { win_e.extended.get_ref() }.uwp, ffi::Option::None);
    }

    #[test]
    fn uwp_info() {
        let proc = Process::start("\"calc.exe\"");
        let mut win_e = proc.main_window().unwrap();
        win_e.extended().unwrap();

        assert_eq!(
            unsafe { win_e.extended.get_ref() }.process.id,
            proc.info.dwProcessId
        );
        assert_eq!(unsafe { win_e.extended.get_ref() }.uwp, ffi::Option::None);
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
            proc2
                .main_window()
                .map(|x| unsafe { x.basic.assume_init() }.hwnd),
            Some(unsafe { ffi::windows::winuser::GetForegroundWindow() })
        );

        proc.switch_to_foreground();

        assert_eq!(
            proc.main_window()
                .map(|x| unsafe { x.basic.assume_init() }.hwnd),
            Some(unsafe { ffi::windows::winuser::GetForegroundWindow() })
        );
    }
}
