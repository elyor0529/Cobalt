struct App {
    id: u64,
    name: String,
    description: String,
    appId: AppId,
    background: String,
    icon: u64 // TODO
}

enum AppId {
    Win32 { Path: String },
    UWP { AUMID: String },
    Java { JarClassArgs: String }
}

struct Session {
    id: u64,
    title: String,
    app: App
}

struct Usage {
    id: u64,
    start: DateTime<Utc>,
    end: DateTime<Utc>,
    session: Session
}
