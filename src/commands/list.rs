use crate::application::AppContext;
use crate::db_data::TimeLogMode;
use crate::fmt::Formatter;
use crate::log::Message::{Error, Normal, Success, Warning};
use crate::time_log_handling::TimeLogExtended;
use chrono::Days;
use clap::{arg, command, value_parser, ArgGroup};

#[derive(Clone, Debug)]
pub struct ListCommand;

impl ListCommand {
    fn cli() -> clap::Command {
        command!()
            .disable_version_flag(true)
            .disable_help_flag(true)
            .disable_help_subcommand(true)
            .name("list")
            .group(ArgGroup::new("list"))
            .arg(
                arg!([DAYS] "The amount of days to get")
                    .required(false)
                    .value_parser(value_parser!(u32).range(1..)),
            )
        // .subcommand(Command::new("list").about("Lists all locations"))
        // .subcommand(Command::new("select").about("Shows a list of locations to select from"))
    }

    async fn inner_execute(&self, command: &String, app: &AppContext) -> Result<bool, sqlx::Error> {
        let matches = Self::cli().try_get_matches_from(
            command
                .split_whitespace()
                .map(|s| s.to_string())
                .collect::<Vec<String>>(),
        );
        if let Ok(matches) = matches {
            let value = matches.try_get_one::<u32>("DAYS");
            if let Err(e) = value {
                app.log.append(Error(format!("Error: {:?}", e).into()));
                return Ok(false);
            }
            let days = value.unwrap().unwrap_or_else(|| &(1u32));
            let to = chrono::Utc::now().naive_utc();
            let from = to.checked_sub_days(Days::new(*days as u64));
            if let None = from {
                app.log.append(Error(format!(
                    "Failed to subtract {} days from {}",
                    days, to
                )));
                return Ok(false);
            }
            let days = crate::db_repository::get_n_days(app.pool.clone(), 0, *days as i64).await?;

            for day in days {
                let time_logs =
                    crate::db_repository::get_time_logs_of_day(app.pool.clone(), day.id).await?;

                let locations =
                    TimeLogExtended::get_locations(app.pool.clone(), time_logs.iter()).await?;
                let projects =
                    TimeLogExtended::get_projects(app.pool.clone(), time_logs.iter()).await?;

                let mut total = chrono::TimeDelta::zero();
                let mut total_billable = chrono::TimeDelta::zero();

                TimeLogExtended::handle_logs(time_logs.iter(), |time_log| {
                    let location = locations
                        .iter()
                        .find(|location| location.id == time_log.time_log.id);
                    let project = projects
                        .iter()
                        .find(|project| project.id == time_log.time_log.project_id);

                    app.log
                        .append(Normal(time_log.time_log.to_display_string_alt(
                            time_log.timestamp_finished,
                            location,
                            project,
                        )));
                    if let Some(end) = time_log.timestamp_finished {
                        let delta = end - time_log.time_log.timestamp_created;
                        total = total + delta;
                        if match time_log.time_log.mode {
                            TimeLogMode::Normal => true,
                            TimeLogMode::Break => false,
                            TimeLogMode::Quit => false,
                            TimeLogMode::Export => true,
                            TimeLogMode::OffTime => true,
                        } {
                            total_billable = total_billable + delta;
                        }
                    }
                });
                app.log.append(Success(format!(
                    "{} {} total to {} (with breaks: {}).",
                    time_logs.len(),
                    if time_logs.len() == 1 {
                        "entry"
                    } else {
                        "entries"
                    },
                    total_billable.default_format(),
                    total.default_format()
                )));
            }
            return Ok(true);
        }
        Self::render_help(app, None);
        Ok(false)
    }

    fn render_help(app: &AppContext, command: Option<&str>) {
        let styled_str = if let Some(command) = command {
            Self::cli()
                .find_subcommand_mut(command)
                .unwrap()
                .render_long_help()
        } else {
            Self::cli().render_long_help()
        };
        for line in styled_str.to_string().lines() {
            app.log.append(Warning(line.to_string().into()));
        }
    }
}

impl crate::command_handler::Command for ListCommand {
    fn name(&self) -> &'static str {
        "list"
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
