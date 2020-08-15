use crate::windows::*;
use crate::*;

#[repr(C)]
#[derive(Debug, Eq, PartialEq)]
pub struct ProcessHandle(pub wintypes::HANDLE);

impl ProcessHandle {
    pub fn readable(id: u32, sync: bool) -> result::Result<ProcessHandle> {
        let handle = expect! (non_null: {
            processthreadsapi::OpenProcess(
                winnt::PROCESS_VM_READ
                    | winnt::PROCESS_QUERY_INFORMATION
                    | if sync { winnt::SYNCHRONIZE } else { 0 },
                0,
                id,
            )
        })?;
        result::Result::Ok(ProcessHandle(handle))
    }

    pub fn read<T: Default>(&self, addr: *mut T) -> result::Result<T> {
        let mut ret: T = Default::default();
        expect!(true: {
            memoryapi::ReadProcessMemory(
                self.0,
                addr as *mut _ as *mut winapi::ctypes::c_void,
                &mut ret as *mut _ as *mut winapi::ctypes::c_void,
                std::mem::size_of::<T>(),
                std::ptr::null_mut(),
            )
        })?;
        result::Result::Ok(ret)
    }

    pub fn read_string(&self, s: ntdef::UNICODE_STRING) -> result::Result<crate::string::String> {
        let mut buf_len = (s.Length / 2) as usize;
        let mut buf = buffer!(buf_len);
        expect!(true: {
            memoryapi::ReadProcessMemory(
                self.0,
                s.Buffer as *mut _ as *mut winapi::ctypes::c_void,
                buf.as_mut_ptr() as *mut _ as *mut winapi::ctypes::c_void,
                buf_len * 2,
                &mut buf_len,
            )
        })?;
        result::Result::Ok(buffer_to_string!(buf, buf_len))
    }
}

impl Drop for ProcessHandle {
    fn drop(&mut self) {
        expect!(true: handleapi::CloseHandle(self.0)).unwrap(); // TODO instead of unwrap, something else?
    }
}
