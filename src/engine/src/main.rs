#![feature(trait_alias)]
#![feature(async_closure)]

pub mod windows;
pub mod watchers;

use tokio::prelude::*;
use tokio::stream::StreamExt;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut fore = watchers::ForegroundWindowWatcher::new()?;

    let watchers = tokio::task::spawn(async move {
        while let Some(item) = fore.stream.next().await {
            println!("stream: {}", item.title);
        }
    });

    tokio::join!(windows::EventLoop::new(), watchers);
    Ok(())
}
