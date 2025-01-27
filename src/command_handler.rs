use crate::application::AppContext;
use crate::db_data::TimeLogMode;
use crate::db_repository;
use crossterm::style::Color;
use ratatui::prelude::{Span, Stylize};
use std::fmt::Debug;

pub trait Command {
    fn name(&self) -> &'static str;
    async fn execute(&self, app: AppContext) -> Option<Span>;
}
#[derive(Clone, Debug)]
pub struct LocationCommand;
impl Command for LocationCommand {
    fn name(&self) -> &'static str {
        "location"
    }

    async fn execute(&self, app: AppContext<'_>) -> Option<Span> {
        todo!()
    }
}

#[derive(Clone, Debug)]
pub enum Commands {
    Location(LocationCommand),
}

impl Commands {
    fn name(&self) -> &'static str {
        match self {
            Commands::Location(location) => location.name(),
        }
    }

    async fn execute(&self, app: AppContext<'_>) -> Option<Span> {
        match self {
            Commands::Location(location) => location.execute(app).await,
        }
    }
}
#[derive(Clone, Debug, Default)]
pub struct CommandHandler {
    commands: Vec<Commands>,
    current_project: String,
    current_location: String
}
impl CommandHandler {
    pub fn set_project(&mut self, project: String) {
        self.current_project = project;
    }
    pub async fn handle_input(&mut self, app: AppContext<'_>, command: String) -> bool {
        if command.is_empty() {
            app.log.append("Empty line cannot be submitted".fg(Color::Red));
            return false;
        }
        if let Some(_first_word) = command.split_whitespace().next() {
            for command in &self.commands {
                let name = command.name();
                if name == _first_word {
                    let span = command.execute(app.clone()).await;
                    if let Some(span) = span {
                        app.log.append(span);
                    }
                    return true;
                }
            }
        }
        let command = command.trim();

        // command is not a command but project log
        // Parse project logging, having either PROJECT:ACTIVITY or just ACTIVITY

        let colon_index = command.find(':');
        let mut project = self.current_project.clone();
        let location = self.current_location.clone();
        let activity: String;
        if let Some(colon_index) = colon_index {
            project = command[..colon_index].to_string();
            self.current_project = project.clone();
            activity = command[(colon_index + 1)..].to_string();
        } else {
            activity = command.to_string();
        }
        if project.is_empty() {
            app.log.append(
                format!(
                    "No project set. Use 'Your Project: {:}' to set a project.",
                    command
                )
                .fg(Color::Red),
            );
            return false;
        }
        if location.is_empty() {
            app.log.append("No location set. Use location command to set the location".fg(Color::Red));
            return false;
        }
        let result =
            db_repository::add_time_log(app.pool, &project, &location, TimeLogMode::Normal, &activity)
                .await;
        if let Err(e) = result {
            app.log.append(format!("SQLite Error: {:?}", e).fg(Color::Red))
        } else {
            let time_log = result.unwrap();

            app.log.append(
                time_log
                    .to_display_string(None, Some(&location), Some(&project))
                    .fg(Color::Green),
            )
        }

        true
    }
}
