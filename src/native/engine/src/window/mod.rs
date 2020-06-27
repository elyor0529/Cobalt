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
    process: crate::process::Basic,
    uwp: ffi_ext::Option<Uwp>
}

#[repr(C)]
#[derive(Debug)]
pub struct Uwp {
    pub aumid: ffi_ext::String
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

pub unsafe fn is_uwp(pid: u32) -> bool {
    let path = crate::process::path_fast(pid);
    path.to_os_string().eq_ignore_ascii_case("C:\\Windows\\System32\\ApplicationFrameHost.exe")
}

pub unsafe fn aumid(hwnd: wintypes::HWND) -> ffi_ext::String {
    let mut property_store: *mut propsys::IPropertyStore = ptr::null_mut();
    shellapi::SHGetPropertyStoreForWindow(
    hwnd, &uuid::IID_IPropertyStore as *const _ as *const _,
    &mut property_store as *mut _ as *mut *mut wintypes::c_void);

    let mut prop: propidl::PROPVARIANT = mem::zeroed();
    (*property_store).GetValue(&propkey::PKEY_AppUserModel_ID as *const _, &mut prop);

    let aumid_ptr = *prop.data.pwszVal();
    ffi_ext::NulString::from_raw(aumid_ptr).to_ustring()
}

#[no_mangle]
pub unsafe fn window_extended(basic: &Basic) -> Extended {
    let (pid, _) = pid_tid(basic.hwnd);
    let uwp = if is_uwp(pid) {
        ffi_ext::Option::Some(Uwp { aumid: aumid(basic.hwnd) })
    } else {
        ffi_ext::Option::None
    };
    Extended { process: crate::process::Basic { id: pid }, uwp }
}
