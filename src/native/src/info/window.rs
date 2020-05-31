use winapi::um::*;
use winapi::shared::*;
use crate::util::*;
use crate::info::process::*;
use std::*;
use std::collections::HashMap;
use std::borrow::Borrow;
use std::os::windows::prelude::*;

pub struct Window {
    id: windef::HWND,
    title: FfiString,
    uwp_aumid: Option<FfiString>
}

#[no_mangle]
pub unsafe fn window_title(hwnd: windef::HWND) -> FfiString {
    let len = winuser::GetWindowTextLengthW(hwnd);
    let mut buf = vec![0u16; len as usize + 1];
    winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), len + 1);
    buf
}

#[no_mangle]
pub unsafe fn window_from_basic(basic: crate::watchers::foreground_window_watcher::BasicWindowInfo) -> Window {
    let uwp_aumid = uwp_aumid(basic.id);
    Window { id: basic.id, title: basic.title, uwp_aumid }
}

#[no_mangle]
pub unsafe fn uwp_aumid(win: windef::HWND) -> Option<FfiString> {
    let pid = process_id_for_window(win);
    let path = process_path_fast(pid);
    if path.eq_ignore_ascii_case("C:\\Windows\\System32\\ApplicationFrameHost.exe") {
        // get aumid
        let mut property_store: *mut winapi::um::propsys::IPropertyStore = ptr::null_mut();
        let property_store_guid = uuid::IID_IPropertyStore;
        let res = shellapi::SHGetPropertyStoreForWindow(win,
            &property_store_guid as *const _ as *const _,
            &mut property_store as *mut _ as *mut *mut winapi::ctypes::c_void);

        let mut prop: propidl::PROPVARIANT = mem::zeroed();
        (*property_store).GetValue(&propkey::PKEY_AppUserModel_ID as *const _, &mut prop);

        let aumid_ptr = *prop.data.pwszVal();
        let aumid_len = len_pwstr(aumid_ptr);
        Some(Vec::from_raw_parts(aumid_ptr, aumid_len, aumid_len))
    }
    else { None }
}

pub unsafe fn len_pwstr(str: *mut u16) -> usize {
    for i in 0.. {
        if *str.offset(i) == 0u16 { return i as usize + 1 }
    }
    0
}

#[repr(C)]
pub struct WindowClosed<'a> {
    sub: &'a Subscription<()>
}

lazy_static! {
    static ref WINDOW_CLOSED_GLOBALS: sync::Mutex<HashMap<usize, WindowClosed<'static>>> = {
        sync::Mutex::new(HashMap::new())
    };
}

#[no_mangle]
pub unsafe fn window_closed_begin(sub: &'static Subscription<()>, win: windef::HWND) -> windef::HWND {
    let mut globals = WINDOW_CLOSED_GLOBALS.lock().unwrap();
    let global = WindowClosed { sub };
    globals.insert(0, global);

    // winuser::SetWinEventHook(winuser::DE);

    win
}
