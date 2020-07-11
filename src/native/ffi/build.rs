::winrt::build!(
    dependencies
        os
    types
        windows::foundation::*
        windows::system::*
        windows::storage::*
        windows::storage::streams::*
);

fn main() {
    build();
}
