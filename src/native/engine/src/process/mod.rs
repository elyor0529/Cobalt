use ffi::windows::*;
use ffi::*;

// pub mod exited;

#[repr(C)]
#[derive(Debug)]
pub struct Basic {
    pub id: u32,
}

#[repr(C)]
#[derive(Debug)]
pub struct Extended {
    pub handle: ProcessHandle,
}

#[repr(C)]
#[derive(Debug)]
pub struct Identification {
    pub path: ffi::String,
    pub cmd_line: ffi::String,
}

#[repr(C)]
#[derive(Debug)]
pub struct FileInfo {
    pub name: ffi::String,
    pub description: ffi::String,
}

#[no_mangle]
pub unsafe fn process_extended(basic: &Basic) -> ffi::Result<Extended> {
    let handle = ProcessHandle::readable(basic.id, true)?;
    ffi::Result::Ok(Extended { handle })
}

#[no_mangle]
pub unsafe fn process_identification(extended: &Extended) -> ffi::Result<Identification> {
    let handle = &extended.handle;
    let mut info: ntpsapi::PROCESS_BASIC_INFORMATION = Default::default();
    let mut info_len = 0u32;
    ntstatus!({
        ntpsapi::NtQueryInformationProcess(
            handle.0,
            0,
            &mut info as *mut _ as *mut ctypes::c_void,
            ffi::size_of::<ntpsapi::PROCESS_BASIC_INFORMATION>() as u32,
            &mut info_len as &mut u32,
        )
    })?;

    let peb = handle.read(info.PebBaseAddress)?;
    let user_params = handle.read(peb.ProcessParameters)?;

    let path = handle.read_string(user_params.ImagePathName)?;
    let cmd_line = handle.read_string(user_params.CommandLine)?;
    ffi::Result::Ok(Identification { path, cmd_line })
}

#[no_mangle]
pub fn process_file_info(identification: &Identification) -> ffi::Result<FileInfo> {
    process_file_info_std(identification).into()
}

fn process_file_info_std(
    identification: &Identification,
) -> std::result::Result<FileInfo, std::boxed::Box<dyn std::error::Error>> {
    // TODO std::result to our Result
    let file_map =
        pelite::FileMap::open(&std::path::Path::new(&identification.path.to_os_string()))?;
    let image = pelite::PeFile::from_bytes(file_map.as_ref())?; //.expect("file is not a PE image");
    let resources = image.resources()?; //.expect("resources not found");
    let version_info = resources.version_info()?; //.expect("version info not found");
    let default_lang = version_info.translation()[0];
    std::iter::once(default_lang)
        .chain(FALLBACK_LANGS.iter().map(|x| lang(x)))
        .find_map(|lang| file_info_from_version_info(version_info, lang))
        .ok_or("No valid name and description found".into())
}

pub static FALLBACK_LANGS: &[&str] = &[
    "040904B0", // US English + CP_UNICODE
    "040904E4", // US English + CP_USASCII
    "04090000", // US English + unknown codepage
];

pub fn file_info_from_version_info(
    version_info: pelite::resources::version_info::VersionInfo,
    lang: pelite::resources::version_info::Language,
) -> std::option::Option<FileInfo> {
    let name = version_info
        .value(lang, "ProductName")
        .map(|x| ffi::String::from(x));
    let desc = version_info
        .value(lang, "FileDescription")
        .map(|x| ffi::String::from(x));
    name.zip(desc)
        .map(|(name, description)| FileInfo { name, description })
}

pub fn lang(s: &'static str) -> pelite::resources::version_info::Language {
    let buf = ffi::String::from_str(s).into_vec();
    pelite::resources::version_info::Language::parse(&buf[..]).expect("Cannot parse language")
}

pub unsafe fn path_fast(handle: &ProcessHandle) -> ffi::String {
    let mut len = 1024u32; // TODO macro this pattern
    let buf = loop {
        let mut buf = ffi::buffer!(len);
        let res = winbase::QueryFullProcessImageNameW(handle.0, 0, buf.as_mut_ptr(), &mut len);
        if res != 0 {
            // TODO check GetLastError, make sure its some error like not enough buffer
            break ffi::buffer_to_string!(&buf[..len as usize]);
        }
        len *= 2;
    };
    buf
}
