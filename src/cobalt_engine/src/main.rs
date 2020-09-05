use winapi::um::*;
use winapi::shared::minwindef::*;
use winapi::shared::windef::*;
use winapi::um::winnt::*;
use async_std::prelude::*;
use crate::winhook::*;
use crate::event_loop::*;
use libffi::high::*;

mod event_loop;
mod winhook;

pub fn title(hwnd: HWND) -> String {
    use std::os::windows::ffi::OsStringExt;

    let len = unsafe { winuser::GetWindowTextLengthW(hwnd) }; // TODO this could error out
    let mut buf = vec![0u16; len as usize];
    unsafe { winuser::GetWindowTextW(hwnd, buf.as_mut_ptr(), len + 1) }; // TODO this could error out
    std::ffi::OsString::from_wide(&buf).into_string().unwrap()
}

struct WindowSwitch {
    title: String
}

fn window_switches<'a>() -> (WinEventHook<'a>, async_std::sync::Receiver<WindowSwitch>) {

    let (sender, recver) = async_std::sync::channel::<WindowSwitch>(1);

    let cb = move |
        _win_event_hook: HWINEVENTHOOK,
        _event: DWORD,
        hwnd: HWND,
        _id_object: LONG,
        _id_child: LONG,
        _id_event_thread: DWORD,
        dwms_event_time: DWORD,
    | {
        if unsafe { winuser::IsWindow(hwnd) == 0 || winuser::IsWindowVisible(hwnd) == 0 } {
            return;
        }
        async_std::task::block_on(sender.send(WindowSwitch { title: title(hwnd) }));
    };

    let hook = WinEventHook::new(
        EventRange::Single(WinEvent::SystemForeground),
        EventLocality::Global,
        cb
    );

    (hook, recver)
}

#[async_std::main]
async fn main() {
    let (hook, mut recv) = window_switches();
    let watcher = async {
        while let Some(item) = recv.next().await {
            println!("switched using stream: {}", item.title);
        }
    };
    watcher.join(EventLoop::new()).await;
}
