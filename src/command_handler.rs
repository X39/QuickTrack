use crate::db_repository;
use sqlx::{Pool, Sqlite};
use std::fmt::Debug;
use crossterm::style::Color;
use ratatui::prelude::{Span, Stylize};
use tokio::runtime::Handle;
use crate::db_data::TimeLogMode;

#[derive(Clone, Debug)]
pub enum Command {
}
#[derive(Clone, Debug, Default)]
pub struct CommandHandler<'a> {
    commands: Vec<Command>,
    current_project: String,
    current_location: String,
    messages: Vec<Span<'a>>,
}
impl CommandHandler<'_> {
    pub fn set_project(&mut self, project: String) {
        self.current_project = project;
    }

    pub fn messages(&self) -> &Vec<Span> {
        &self.messages
    }
    pub fn handle_input(&mut self, pool: Pool<Sqlite>, command: String) -> bool {
        if let Some(_first_word) = command.split_whitespace().next() {
            // ToDo: Add commands
        }

        // command is not a command but project log
        // Parse project logging, having either PROJECT:ACTIVITY or just ACTIVITY

        let colon_index = command.find(':');
        let mut project = self.current_project.clone();
        let mut location = self.current_location.clone();
        let activity: String;
        if let Some(colon_index) = colon_index {
            project = command[..colon_index].to_string();
            activity = command[(colon_index + 1)..].to_string();
        } else {
            activity = command;
        }
        let handle = Handle::current();
        let result = handle.block_on(async {
            db_repository::add_time_log(pool, project, location, TimeLogMode::Normal, activity.clone()).await
        });
        if let Err(e) = result {
            self.messages.push(format!("SQLite Error: {:?}", e).fg(Color::Red))
        } else {
            self.messages.push(activity.fg(Color::Green))
        }

        true
    }
}
