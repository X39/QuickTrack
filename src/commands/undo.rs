use chrono::Days;
use clap::{arg, command, value_parser, ArgGroup};
use crate::application::AppContext;
use crate::db_data::TimeLogMode;
use crate::fmt::Formatter;
use crate::log::Message::{Error, Failure, Normal, Success, Warning};
use crate::time_log_handling::TimeLogExtended;

#[derive(Clone, Debug)]
pub struct UndoCommand;

impl UndoCommand {

    async fn inner_execute(&self, command: &String, app: &AppContext) -> Result<bool, sqlx::Error> {
        if command != "undo" {
            app.log.append(Error("Undo command does not accept any arguments".to_string()));
            return Ok(false);
        }
        let latest_time_logs = crate::db_repository::get_n_time_logs_id_desc(app.pool.clone(), 0, 1).await?;
        if latest_time_logs.len() == 0 {
            app.log.append(Failure("No time logs to undo".to_string()));
        } else {
            let first = latest_time_logs.first().unwrap();
            let location = crate::db_repository::get_location_by_id(app.pool.clone(), first.location_id).await?;
            let project = crate::db_repository::get_project_by_id(app.pool.clone(), first.project_id).await?;
            crate::db_repository::drop_time_log(app.pool.clone(), first.id).await?;
            app.log.append(Success(format!("Undid {}", first.to_display_string_alt(None, location.as_ref(), project.as_ref()))));
        }
        Ok(true)
    }

}

impl crate::command_handler::Command for UndoCommand {
    fn name(&self) -> &'static str {
        "undo"
    }

    async fn execute(&self, command: &String, app: &AppContext) -> bool {
        let result = self.inner_execute(command, app).await;
        if let Err(result) = result {
            app.log.append(Error(format!("SQLite Error: {:?}", result)));
            return false;
        }
        result.unwrap()
    }
}
