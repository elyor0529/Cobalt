use std::*;
use ffi_ext::win32::*;

#[repr(C)]
pub struct Basic {
    hwnd: windef::HWND,
    title: ffi_ext::String
}

#[repr(C)]
pub struct Extended {
    uwp_aumid: ffi_ext::Nullable<ffi_ext::String>
}

/*#[no_mangle]
pub unsafe fn title(hwnd: windef::HWND) -> ffi_ext::String {
    let len = winuser::GetWindowTextLengthW(hwnd);
    let mut buf = vec![0u16; len as usize+1];
    winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), len+1);
    buf.set_len(len as usize); // Do not include the u16 null byte at the end
    buf
}*/
/*


#[repr(C)]
#[derive(Debug)]
pub struct WindowClosed {
    sub: Subscription<()>
}

lazy_static! {
    static ref WINDOW_CLOSED_GLOBALS: sync::Mutex<HashMap<Ptr<windef::HWND>, WindowClosed>> = {
        sync::Mutex::new(HashMap::new())
    };
}

unsafe extern "system" fn window_closed_handler(
    win_event_hook: windef::HWINEVENTHOOK,
    _event: minwindef::DWORD,
    hwnd: windef::HWND,
    id_object: winnt::LONG,
    id_child: winnt::LONG,
    _id_event_thread: minwindef::DWORD,
    _dwms_event_time: minwindef::DWORD) {
    if id_object != winuser::OBJID_WINDOW || id_child != 0 { return; }
    let key = &Ptr(hwnd);

    let mut lock = WINDOW_CLOSED_GLOBALS.lock().unwrap();
    let closed = lock.get(key);
    if let Some(c) = &closed {
        (c.sub.on_next)(&());
        (c.sub.on_complete)();
        winuser::UnhookWinEvent(win_event_hook);
        lock.remove(key);
    }
}

#[no_mangle]
pub unsafe fn window_closed_begin(sub: Subscription<()>, win: windef::HWND) -> windef::HWND {
    let mut globals = WINDOW_CLOSED_GLOBALS.lock().unwrap();
    let global = WindowClosed { sub };
    globals.insert(Ptr(win), global);

    let mut pid = 0;
    let tid = winuser::GetWindowThreadProcessId(win, &mut pid);
    let _hook = winuser::SetWinEventHook(
        winuser::EVENT_OBJECT_DESTROY,
        winuser::EVENT_OBJECT_DESTROY,
        ptr::null_mut(),
        Some(window_closed_handler),
        pid, tid,
        winuser::WINEVENT_OUTOFCONTEXT);

    win
}*/
