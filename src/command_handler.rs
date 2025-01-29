use crate::application::AppContext;
use crate::commands::Commands;
use crate::db_data::TimeLogMode;
use crate::db_repository;
use crate::log::Message::{Error, Failure, Success};
use std::fmt::Debug;

pub trait Command {
    fn name(&self) -> &'static str;
    async fn execute(&self, command: &String, app: &AppContext) -> bool;
}
#[derive(Clone, Debug)]
pub struct CommandHandler {
    commands: Vec<Commands>,
}
impl CommandHandler {
    pub fn new() -> Self {
        Self {
            commands: Commands::create_all(),
        }
    }
    pub async fn handle_input(&mut self, app: &AppContext, user_input: &String) -> bool {
        if user_input.is_empty() {
            app.log
                .append(Failure("Empty line cannot be submitted".into()));
            return false;
        }
        if let Some(_first_word) = user_input.split_whitespace().next() {
            for command in &self.commands {
                let name = command.name();
                if name == _first_word {
                    return command.execute(user_input, app).await;
                }
            }
        }
        let command = user_input.trim();

        // command is not a command but project log
        // Parse project logging, having either PROJECT:ACTIVITY or just ACTIVITY

        let colon_index = command.find(':');

        // Load the active project
        let mut project = db_repository::get_active_project(app.pool.clone()).await;
        if let Err(e) = project {
            app.log.append(Error(format!("SQLite Error: {:?}", e).into()));
            return false;
        }
        let mut project = project.unwrap().map_or(
            String::from(""),
            |p| p.title.clone());

        // Load the active location
        let mut location = db_repository::get_active_location(app.pool.clone()).await;
        if let Err(e) = location {
            app.log.append(Error(format!("SQLite Error: {:?}", e).into()));
            return false;
        }
        let location = location.unwrap().map_or(
            String::from(""),
            |p| p.title.clone());
        if location.is_empty() {
            app.log.append(Failure(
                "No location set. Use location command to set the location.".into(),
            ));
            return false;
        }
        

        let activity: String;
        if let Some(colon_index) = colon_index {
            project = command[..colon_index].to_string();
            if let Err(e) = db_repository::set_active_project(app.pool.clone(), &project).await {
                app.log.append(Error(format!("SQLite Error: {:?}", e).into()));
                return false;
            }
            activity = command[(colon_index + 1)..].to_string();
        } else {
            activity = command.to_string();
        }
        if project.is_empty() {
            app.log.append(Failure(
                format!(
                    "No project set. Use 'Your Project: {:}' to set a project.",
                    command
                )
                .into(),
            ));
            return false;
        }
        let result = db_repository::add_time_log(
            app.pool.clone(),
            &project,
            &location,
            TimeLogMode::Normal,
            &activity,
        )
        .await;
        if let Err(e) = result {
            app.log
                .append(Error(format!("SQLite Error: {:?}", e).into()))
        } else {
            let time_log = result.unwrap();

            app.log.append(Success(time_log.to_display_string(
                None,
                Some(&location),
                Some(&project),
            )))
        }

        true
    }
}
