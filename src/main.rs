mod application;
mod default_window;

use crate::application::Application;
use crate::default_window::DefaultWindow;
use crossterm::event;
use crossterm::event::{poll, Event, KeyCode, KeyEventKind};
use ratatui::prelude::Stylize;
use ratatui::widgets::Paragraph;
use ratatui::{DefaultTerminal, Frame};
use std::io;
use std::time::Duration;

fn main() -> io::Result<()> {
    let mut terminal = ratatui::init();
    terminal.clear()?;
    let mut app = Application::new();
    app.push_window(Box::new(DefaultWindow {
        ..Default::default()
    }));
    let app_result = app.application_loop(terminal);
    ratatui::restore();
    app_result
}