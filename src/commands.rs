use crate::application::AppContext;
use crate::command_handler::{Command, CommandHandler};
use crate::commands::location::LocationCommand;

pub mod location;

#[derive(Clone, Debug)]
pub enum Commands {
    Location(LocationCommand),
}

impl Commands {
    pub fn name(&self) -> &'static str {
        match self {
            Commands::Location(location) => location.name(),
        }
    }

    pub async fn execute(&self, command: &String, app: &AppContext) -> bool {
        match self {
            Commands::Location(location) => location.execute(command, app).await,
        }
    }
    pub fn create_all() -> Vec<Commands> {
        vec![Commands::Location(LocationCommand)]
    }
}
