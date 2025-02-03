mod application;
mod command_handler;
mod commands;
mod db_data;
mod db_repository;
mod default_window;
mod fmt;
mod log;
mod time_log_handling;

use crate::application::{Application, Windows};
use crate::default_window::DefaultWindow;
use std::io;

#[tokio::main]
async fn main() -> io::Result<()> {
    // Prepare SQLite connection
    let pool = sqlx::sqlite::SqlitePoolOptions::new()
        .max_connections(1)
        .connect("sqlite:quicktrack.db")
        .await;
    if pool.is_err() {
        panic!("Failed to connect to database quicktrack.db");
    }
    let pool = pool.unwrap();

    // Prepare terminal
    let mut terminal = ratatui::init();
    terminal.clear()?;

    // Prepare app
    let mut app = Application::new(pool);

    // Add default window
    let mut wnd = DefaultWindow::new();
    let app_context = app.context();
    wnd.update_project_from_db(&app_context).await;
    wnd.update_location_from_db(&app_context).await;
    app.push_window(Windows::Default(wnd));

    let app_result = app.application_loop(terminal).await;
    ratatui::restore();
    app_result
}
