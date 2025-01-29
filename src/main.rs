mod command_handler;
mod application;
mod db_repository;
mod default_window;
mod db_data;
mod log;
mod commands;

use crate::application::{Application, Windows};
use crate::default_window::DefaultWindow;
use std::io;

#[tokio::main]
async fn main() -> io::Result<()> {
    let pool = sqlx::sqlite::SqlitePoolOptions::new()
        .max_connections(1)
        .connect("sqlite:quicktrack.db")
        .await;
    if pool.is_err() {
        panic!("Failed to connect to database quicktrack.db");
    }
    let pool = pool.unwrap();
    let mut terminal = ratatui::init();
    terminal.clear()?;
    let mut app = Application::new(pool);
    app.push_window(Windows::Default(DefaultWindow::new()));
    let app_result = app.application_loop(terminal).await;
    ratatui::restore();
    app_result
}
