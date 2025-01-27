use ratatui::prelude::Span;
use std::sync::{Arc, Mutex};

#[derive(Clone)]
pub struct Log<'a>(Arc<Mutex<LogManager<'a>>>);
struct LogManager<'a> {
    messages: Vec<Span<'a>>,
}
impl Log<'_> {
    pub fn create_new() -> Self {
        let log_manager = LogManager { messages: vec![] };
        Log(Arc::new(Mutex::new(log_manager)))
    }

    pub fn messages_length(&self) -> usize {
        self.0.lock().unwrap().messages.len()
    }

    pub fn messages(&self) -> Vec<Span> {
        self.0.lock().unwrap().messages.clone()
    }
    pub fn append(& self, span: Span) {
        let lock = self.0.lock();
        let mut log_manager = lock.unwrap();
        log_manager.messages.push(span.clone());
    }
}
