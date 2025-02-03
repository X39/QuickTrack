use crate::application::AppContext;
use crate::log::Message::{Error, Failure, Success, Warning};
use clap::{arg, command, ArgGroup, ArgMatches, Command};

#[derive(Clone, Debug)]
pub struct LocationCommand;

impl LocationCommand {
    fn cli() -> clap::Command {
        command!()
            .disable_version_flag(true)
            .disable_help_flag(true)
            .disable_help_subcommand(true)
            .name("location")
            .group(ArgGroup::new("location"))
            .subcommand(
                Command::new("set")
                    .about("Set the active location")
                    .arg(arg!([VALUE] "The location to set as active").required(true)),
            )
            .subcommand(Command::new("get").about("Checks what is set as the active location"))
            .subcommand(
                Command::new("add")
                    .about("Adds a new location and makes it active")
                    .arg(arg!([VALUE] "The location to add and set as active").required(true)),
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
            if let Some(matches) = matches.subcommand_matches("set") {
                let result = Self::execute_set(app, matches).await?;
                if !result {
                    Self::render_help(app, Some("set"));
                }
                return Ok(result);
            }
            if let Some(_) = matches.subcommand_matches("get") {
                let result = Self::execute_get(app).await?;
                if !result {
                    Self::render_help(app, Some("get"));
                }
                return Ok(result);
            }
            if let Some(matches) = matches.subcommand_matches("add") {
                let result = Self::execute_add(app, matches).await?;
                if !result {
                    Self::render_help(app, Some("add"));
                }
                return Ok(result);
            }
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

    async fn execute_add(app: &AppContext, set_results: &ArgMatches) -> Result<bool, sqlx::Error> {
        let value = set_results.get_one::<String>("VALUE");
        if let Some(value) = value {
            let location =
                crate::db_repository::get_or_add_location(app.pool.clone(), &value).await?;
            crate::db_repository::set_active_location(app.pool.clone(), &location.title).await?;
            app.log
                .append(Success(format!("Location set to: {}", location.title)));
            Ok(true)
        } else {
            app.log
                .append(Error("No value provided for location set command".into()));
            Ok(false)
        }
    }
    async fn execute_set(app: &AppContext, set_results: &ArgMatches) -> Result<bool, sqlx::Error> {
        let value = set_results.get_one::<String>("VALUE");
        if let Some(value) = value {
            let location = crate::db_repository::get_location(app.pool.clone(), &value).await?;
            if let Some(location) = location {
                crate::db_repository::set_active_location(app.pool.clone(), &location.title)
                    .await?;
                app.log
                    .append(Success(format!("Location set to: {}", location.title)));
                Ok(true)
            } else {
                app.log
                    .append(Failure(format!("No location found with name: {}", value)));
                Ok(false)
            }
        } else {
            app.log
                .append(Error("No value provided for location set command".into()));
            Ok(false)
        }
    }

    async fn execute_get(app: &AppContext) -> Result<bool, sqlx::Error> {
        let location = crate::db_repository::get_active_location(app.pool.clone()).await?;
        if let Some(location) = location {
            app.log
                .append(Success(format!("Active location: {}", location.title)));
            Ok(true)
        } else {
            app.log.append(Failure("No location set".into()));
            Ok(false)
        }
    }
}
impl crate::command_handler::Command for LocationCommand {
    fn name(&self) -> &'static str {
        "location"
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
