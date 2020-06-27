use ffi_ext::win32::*;
use std::*;

mod foreground;
mod closed;

#[repr(C)]
#[derive(Debug)]
pub struct Basic {
    pub hwnd: wintypes::HWND,
    pub title: ffi_ext::String
}

#[repr(C)]
#[derive(Debug)]
pub struct Extended {
    pid: u32,
    uwp_aumid: ffi_ext::Option<ffi_ext::String>
}

pub unsafe fn title(hwnd: wintypes::HWND) -> ffi_ext::String {
    let len = winuser::GetWindowTextLengthW(hwnd);
    let mut buf = ffi_ext::buffer!(len + 1);
    winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), len+1);
    buf.set_len(len as usize); // do not include the u16 null byte at the end
    ffi_ext::buffer_to_string!(buf)
}

pub fn pid_tid(hwnd: wintypes::HWND) -> (u32, u32) {
    let mut pid = 0;
    let tid = unsafe { winuser::GetWindowThreadProcessId(hwnd, &mut pid) };
    (pid, tid)
}

/*
#[no_mangle]
pub unsafe fn uwp_aumid(win: windef::HWND) -> Option<FfiString> {
    let pid = process_id_for_window(win);
    let path = process_path_fast(pid);
    if !path.eq_ignore_ascii_case("C:\\Windows\\System32\\ApplicationFrameHost.exe") { return None }

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
*/
