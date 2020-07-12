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

#[repr(C)]
#[derive(Debug)]
pub struct Window {
    pub basic: std::mem::MaybeUninit<Basic>,
    pub extended: std::mem::MaybeUninit<Extended>,
}

impl Window {
    pub fn title(hwnd: wintypes::HWND) -> ffi::String {
        let len = unsafe { winuser::GetWindowTextLengthW(hwnd) }; // TODO this could error out
        let mut buf = ffi::buffer!(len + 1);
        unsafe { winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), len + 1) }; // TODO this could error out
        ffi::buffer_to_string!(buf, len)
    }

    pub fn pid_tid(hwnd: wintypes::HWND) -> ffi::Result<(u32, u32)> {
        let mut pid = 0;
        let tid = unsafe { winuser::GetWindowThreadProcessId(hwnd, &mut pid) };
        if pid == 0 || tid == 0 {
            ffi::Result::Err(Error::last_win32())
        } else {
            ffi::Result::Ok((pid, tid))
        }
    }

    pub fn is_uwp(pid: u32) -> ffi::Result<bool> {
        let handle = ProcessHandle::readable(pid, false)?;
        ffi::Result::Ok(if unsafe { winuser::IsImmersiveProcess(handle.0) } != 0 {
            let path = crate::process::path_fast(&handle);
            path.to_os_string()
                .eq_ignore_ascii_case("C:\\Windows\\System32\\ApplicationFrameHost.exe")
        // double check
        } else {
            false
        })
    }

    pub fn aumid(hwnd: wintypes::HWND) -> ffi::Result<ffi::String> {
        let mut property_store: *mut propsys::IPropertyStore = ptr::null_mut();
        hresult!({
            shellapi::SHGetPropertyStoreForWindow(
                hwnd,
                &uuid::IID_IPropertyStore as *const _ as *const _,
                &mut property_store as *mut _ as *mut *mut wintypes::c_void,
            )
        })?;

        let mut prop: propidl::PROPVARIANT = Default::default();
        hresult!((*property_store).GetValue(&propkey::PKEY_AppUserModel_ID as *const _, &mut prop))?;

        let aumid_ptr = unsafe { *prop.data.pwszVal() };
        ffi::Result::Ok(unsafe { ffi::NulString::from_raw(aumid_ptr).to_ustring() })
    }

    #[no_mangle]
    pub fn extended(&mut self) -> ffi::Result<()> {
        let hwnd = unsafe { (&self.basic).get_ref() }.hwnd;
        let (pid, _) = Window::pid_tid(hwnd)?;
        let uwp = if Window::is_uwp(pid)? {
            ffi::Option::Some(Uwp {
                aumid: Window::aumid(hwnd)?,
            })
        } else {
            ffi::Option::None
        };
        self.extended.write(Extended {
            process: crate::process::Basic { id: pid },
            uwp,
        });
        ffi::Result::Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn lol() {}
}
