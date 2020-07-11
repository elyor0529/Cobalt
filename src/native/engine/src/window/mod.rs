use ffi::windows::*;
use ffi::*;

// pub mod closed;
// pub mod foreground;

#[repr(C)]
#[derive(Debug)]
pub struct Basic {
    pub hwnd: wintypes::HWND,
    pub title: ffi::String,
}

#[repr(C)]
#[derive(Debug)]
pub struct Extended {
    pub process: crate::process::Basic,
    pub uwp: ffi::Option<Uwp>,
}

#[repr(C)]
#[derive(Debug, Ord, PartialOrd, Eq, PartialEq)]
pub struct Uwp {
    pub aumid: ffi::String,
}

pub unsafe fn title(hwnd: wintypes::HWND) -> ffi::String {
    let len = winuser::GetWindowTextLengthW(hwnd);
    let mut buf = ffi::buffer!(len + 1);
    winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), len + 1);
    ffi::buffer_to_string!(buf, len)
}

pub fn pid_tid(hwnd: wintypes::HWND) -> (u32, u32) {
    let mut pid = 0;
    let tid = unsafe { winuser::GetWindowThreadProcessId(hwnd, &mut pid) };
    (pid, tid)
}

pub unsafe fn is_uwp(pid: u32) -> ffi::Result<bool> {
    let handle = ProcessHandle::readable(pid, false)?;
    ffi::Result::Ok(if winuser::IsImmersiveProcess(handle.0) != 0 {
        let path = crate::process::path_fast(&handle);
        path.to_os_string()
            .eq_ignore_ascii_case("C:\\Windows\\System32\\ApplicationFrameHost.exe")
    // double check
    } else {
        false
    })
}

pub unsafe fn aumid(hwnd: wintypes::HWND) -> ffi::Result<ffi::String> {
    let mut property_store: *mut propsys::IPropertyStore = ptr::null_mut();
    hresult!(shellapi::SHGetPropertyStoreForWindow(
        hwnd,
        &uuid::IID_IPropertyStore as *const _ as *const _,
        &mut property_store as *mut _ as *mut *mut wintypes::c_void
    ))?;

    let mut prop: propidl::PROPVARIANT = Default::default();
    hresult!((*property_store).GetValue(&propkey::PKEY_AppUserModel_ID as *const _, &mut prop))?;

    let aumid_ptr = *prop.data.pwszVal();
    ffi::Result::Ok(ffi::NulString::from_raw(aumid_ptr).to_ustring())
}

#[no_mangle]
pub unsafe fn window_extended(basic: &Basic) -> ffi::Result<Extended> {
    let (pid, _) = pid_tid(basic.hwnd);
    let uwp = if is_uwp(pid)? {
        ffi::Option::Some(Uwp {
            aumid: aumid(basic.hwnd)?,
        })
    } else {
        ffi::Option::None
    };
    ffi::Result::Ok(Extended {
        process: crate::process::Basic { id: pid },
        uwp,
    })
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn lol() {}
}
