use crate::application::AppContext;
use crate::command_handler::Command;
use crate::commands::list::ListCommand;
use crate::commands::location::LocationCommand;
use crate::commands::undo::UndoCommand;

mod list;
pub mod location;
mod undo;

#[derive(Clone, Debug)]
pub enum Commands {
    Location(LocationCommand),
    List(ListCommand),
    Undo(UndoCommand),
}

impl Commands {
    pub fn name(&self) -> &'static str {
        match self {
            Commands::Location(cmd) => cmd.name(),
            Commands::List(cmd) => cmd.name(),
            Commands::Undo(cmd) => cmd.name(),
        }
    }

    pub async fn execute(&self, command: &String, app: &AppContext) -> bool {
        match self {
            Commands::Location(cmd) => cmd.execute(command, app).await,
            Commands::List(cmd) => cmd.execute(command, app).await,
            Commands::Undo(cmd) => cmd.execute(command, app).await,
        }
    }
    pub fn create_all() -> Vec<Commands> {
        vec![
            Commands::Location(LocationCommand),
            Commands::List(ListCommand),
            Commands::Undo(UndoCommand),
        ]
    }
}
