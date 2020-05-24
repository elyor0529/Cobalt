mod watchers;

struct Observable<T> {
    on_next: extern "stdcall" fn(T),
    on_error: extern "stdcall" fn(u32),
    on_complete: extern "stdcall" fn(),
    drop: extern "stdcall" fn()
}

#[no_mangle]
fn interval(obs: &Observable<u32>) {
    for i in 0..5 {
        obs.on_next(i);
    }
}
