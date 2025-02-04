use crate::application::AppContext;
use crate::commands::Commands;
use crate::db_data::{Day, TimeLogMode};
use crate::db_repository;
use crate::log::Message;
use crate::log::Message::{Error, Failure, Success};
use chrono::{DateTime, Datelike, Local, MappedLocalTime, NaiveDate, NaiveDateTime, NaiveTime, TimeDelta, TimeZone};
use std::fmt::{format, Debug};
use std::ops::Add;

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
        let project = db_repository::get_active_project(app.pool.clone()).await;
        if let Err(e) = project {
            app.log
                .append(Error(format!("SQLite Error: {:?}", e).into()));
            return false;
        }
        let mut project = project
            .unwrap()
            .map_or(String::from(""), |p| p.title.clone());

        // Load the active location
        let location = db_repository::get_active_location(app.pool.clone()).await;
        if let Err(e) = location {
            app.log
                .append(Error(format!("SQLite Error: {:?}", e).into()));
            return false;
        }
        let location = location
            .unwrap()
            .map_or(String::from(""), |p| p.title.clone());
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
                app.log
                    .append(Error(format!("SQLite Error: {:?}", e).into()));
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
        if !Self::close_previous_day_if_applicable(app).await {
            return false;
        }
        let result = db_repository::add_time_log(
            app.pool.clone(),
            &project,
            &location,
            TimeLogMode::Normal,
            &activity,
            None,
            None,
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

    async fn close_previous_day_if_applicable(app: &AppContext) -> bool {
        let now = chrono::Local::now();
        let day = db_repository::try_get_day(
            app.pool.clone(),
            now.year() as u16,
            now.month() as u16,
            now.day() as u16,
        )
        .await;
        if let Err(e) = day {
            app.log
                .append(Error(format!("SQLite Error: {:?}", e).into()));
            return false;
        }
        let day = day.unwrap();
        if day.is_none() {
            return true;
        }
        let day = day.unwrap();

        let previous_time_log =
            db_repository::get_n_time_logs_id_desc(app.pool.clone(), 0, 1).await;
        if let Err(e) = previous_time_log {
            app.log
                .append(Error(format!("SQLite Error: {:?}", e).into()));
            return false;
        }
        let previous_time_log = previous_time_log.unwrap();
        if previous_time_log.len() == 0 {
            return true;
        }
        let previous_time_log = &previous_time_log[0];
        if previous_time_log.day_id != day.id && previous_time_log.mode != TimeLogMode::Quit {
            app.log.append(Message::Info(format!(
                "Adding implicit quit to previous day {:0>4}-{:0>2}-{:0>2}",
                day.year, day.month, day.day
            )));
            let previous_day =
                db_repository::get_day_by_id(app.pool.clone(), previous_time_log.day_id).await;
            if let Err(e) = previous_day {
                app.log
                    .append(Error(format!("SQLite Error: {:?}", e).into()));
                return false;
            }
            let previous_day = previous_day.unwrap();
            if previous_day.is_none() {
                app.log.append(Error(format!(
                    "Day with id {} not found",
                    previous_time_log.day_id
                )));
                return false;
            }
            let previous_day = previous_day.unwrap();
            let location =
                db_repository::get_location_by_id(app.pool.clone(), previous_time_log.location_id)
                    .await;
            if let Err(e) = location {
                app.log
                    .append(Error(format!("SQLite Error: {:?}", e).into()));
                return false;
            }
            let location = location.unwrap();
            if location.is_none() {
                app.log.append(Failure(format!(
                    "Location with id {} not found",
                    previous_time_log.location_id
                )));
                return false;
            }
            let location = location.unwrap();
            let project =
                db_repository::get_project_by_id(app.pool.clone(), previous_time_log.project_id)
                    .await;
            if let Err(e) = project {
                app.log
                    .append(Error(format!("SQLite Error: {:?}", e).into()));
                return false;
            }
            let project = project.unwrap();
            if project.is_none() {
                app.log.append(Failure(format!(
                    "Project with id {} not found",
                    previous_time_log.project_id
                )));
                return false;
            }
            let project = project.unwrap();
            let end_of_day_timestamp = NaiveDate::from_ymd_opt(
                previous_day.year as i32,
                previous_day.month as u32,
                previous_day.day as u32,
            );
            if end_of_day_timestamp.is_none() {
                app.log.append(Failure(format!(
                    "Failed to create NaiveDate for {:0>4}-{:0>2}-{:0>2}",
                    previous_day.year, previous_day.month, previous_day.day
                )));
                return false;
            }
            let end_of_day_timestamp: NaiveDateTime = end_of_day_timestamp.unwrap().and_time(NaiveTime::default());
            let end_of_day_timestamp = end_of_day_timestamp
                .add(TimeDelta::days(1))
                .add(TimeDelta::seconds(-1));
            let end_of_day_local_timestamp_opt = Local.from_local_datetime(&end_of_day_timestamp);
            let mut end_of_day_local_timestamp = end_of_day_local_timestamp_opt.single();
            if end_of_day_local_timestamp.is_none() {
                end_of_day_local_timestamp = end_of_day_local_timestamp_opt.earliest();
                if end_of_day_local_timestamp.is_none() {
                    end_of_day_local_timestamp = end_of_day_local_timestamp_opt.latest();
                    if end_of_day_local_timestamp.is_none() {
                        app.log.append(Failure(format!(
                            "Failed to create LocalTime for {:0>4}-{:0>2}-{:0>2}",
                            previous_day.year, previous_day.month, previous_day.day
                        )));
                        return false;
                    }
                }
            }
            let end_of_day_local_timestamp = end_of_day_local_timestamp.unwrap();

            let result = db_repository::add_time_log(
                app.pool.clone(),
                &project.title,
                &location.title,
                TimeLogMode::Quit,
                &"Daybreak".to_string(),
                Some(end_of_day_local_timestamp),
                Some(previous_time_log.day_id),
            )
            .await;
            if let Err(e) = result {
                app.log
                    .append(Error(format!("SQLite Error: {:?}", e).into()))
            }
            let start_of_day_timestamp = end_of_day_local_timestamp.add(TimeDelta::seconds(1));
            let result = db_repository::add_time_log(
                app.pool.clone(),
                &project.title,
                &location.title,
                previous_time_log.mode,
                &previous_time_log.message,
                Some(start_of_day_timestamp),
                Some(day.id),
            ).await;
            if let Err(e) = result {
                app.log
                    .append(Error(format!("SQLite Error: {:?}", e).into()))
            }
        }
        true
    }
}
