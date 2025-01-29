use std::sync::{Arc, Mutex};

#[derive(Clone)]
pub struct Log(Arc<Mutex<LogManager>>);

#[derive(Clone)]
pub enum Message {
    // # Summary
    // Used to document work performed
    // # Default Color Scheme
    // Represented in a dim color (gray) on a default background (black)
    Log(String),

    // # Summary
    // Normal, user-related message.
    // # Default Color Scheme
    // Represented in a bright color (white) on a default background (black)
    Normal(String),
    // # Summary
    // Informative message that should stand out.
    // # Default Color Scheme
    // Represented in a blue color (LightBlue) on a default background (black)
    Info(String),
    // # Summary
    // Message containing a warning that is informing the user about something not being correct.
    // # Default Color Scheme
    // Represented in a yellow color (LightYellow) on a default background (black)
    Warning(String),
    // # Summary
    // Error message that logs hard-errors.
    // For example, Writing to the database failed.
    // # Default Color Scheme
    // Represented in a bright color (white) on a red background (red)
    Error(String),
    // # Summary
    // A successful message to indicate the user that something worked as intended.
    // For example, informing the user that his modifications where stored correctly.
    // # Default Color Scheme
    // Represented in a green color (green) on a default background (black)
    Success(String),
    // # Summary
    // A message indicating something not being correct.
    // Do note that this is a weak error.
    // # Default Color Scheme
    // Represented in a red color (red) on a default background (black)
    Failure(String),
}
struct LogManager {
    messages: Vec<Message>,
}
impl Log {
    pub fn create_new() -> Self {
        let log_manager = LogManager { messages: vec![] };
        Log(Arc::new(Mutex::new(log_manager)))
    }

    pub fn messages_length(&self) -> usize {
        self.0.lock().unwrap().messages.len()
    }

    pub fn messages(&self) -> Vec<Message> {
        self.0.lock().unwrap().messages.clone()
    }

    pub fn append(&self, msg: Message) {
        let lock = self.0.lock();
        let mut log_manager = lock.unwrap();
        log_manager.messages.push(msg);
    }
}
