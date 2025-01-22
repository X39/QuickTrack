use crate::default_window::DefaultWindow;
use crossterm::event;
use crossterm::event::{poll, Event, KeyEvent, MouseEvent};
use ratatui::prelude::{Stylize, Widget};
use ratatui::widgets::Paragraph;
use ratatui::{DefaultTerminal, Frame};
use sqlx::{Pool, Sqlite};
use std::io;
use std::time::Duration;

pub trait Window {
    /// Renders the window
    fn render(self: &mut Self, pframe: &mut Frame);

    /// Performs an update step if needed
    async fn step(self: &mut Self, pool: Pool<Sqlite>);

    /// The terminal gained focus
    async fn on_focus_gained(self: &mut Self, pool: Pool<Sqlite>);

    /// The terminal lost focus
    async fn on_focus_lost(self: &mut Self, pool: Pool<Sqlite>);

    /// A single key event with additional pressed modifiers.
    async fn on_key(self: &mut Self, pool: Pool<Sqlite>, key: KeyEvent);

    /// A single mouse event with additional pressed modifiers.
    async fn on_mouse(self: &mut Self, pool: Pool<Sqlite>, mouse: MouseEvent);

    /// A string that was pasted into the terminal. Only emitted if bracketed paste has been
    /// enabled.
    async fn on_paste(self: &mut Self, pool: Pool<Sqlite>, data: String);

    /// A resize event with new dimensions after resize (columns, rows).
    /// **Note** that resize events can occur in batches.
    async fn on_resize(self: &mut Self, pool: Pool<Sqlite>, columns: u16, rows: u16);
}
#[derive(Clone, Debug)]
pub enum Windows<'a> {
    Default(DefaultWindow<'a>),
}
pub struct Application<'a> {
    exit: bool,
    active_window: usize,
    windows: Vec<Windows<'a>>,
    pool: Pool<Sqlite>,
}

impl Application<'_> {
    pub fn push_window(&mut self, window: Windows) {
        let index = self.windows.len();
        self.windows.push(window);
        self.active_window = index;
    }

    pub fn new(pool: Pool<Sqlite>) -> Self {
        Application {
            pool,
            exit: false,
            active_window: 0,
            windows: Vec::new(),
        }
    }
    pub async fn application_loop(&mut self, mut terminal: DefaultTerminal) -> io::Result<()> {
        while !self.exit {
            // Render
            terminal.draw(|frame| {
                if let Some(active_window) = self.windows.get_mut(self.active_window) {
                    match active_window {
                        Windows::Default(default_window) => default_window.render(frame),
                    }
                }
            })?;

            // Update
            for index in 0..self.windows.len() {
                let window = &mut self.windows[index];
                match window {
                    Windows::Default(default_window) => {
                        default_window.step(self.pool.clone()).await
                    }
                }
            }

            // Events
            if poll(Duration::from_millis(1000 / 60))? {
                self.handle_events(event::read()?).await?;
            }
        }
        Ok(())
    }

    fn render(&self, frame: &mut Frame) {
        let greeting = Paragraph::new("Hello Ratatui! (press 'q' to quit)")
            .white()
            .on_blue();
        frame.render_widget(greeting, frame.area());
    }

    async fn handle_events(&mut self, event: Event) -> io::Result<()> {
        let pool = self.pool.clone();
        let active_window = self.get_active_window();
        match active_window {
            None => {
                self.exit = true;
            }
            Some(window) => match window {
                Windows::Default(window) => {
                    match event {
                        Event::FocusGained => window.on_focus_gained(pool).await,
                        Event::FocusLost => window.on_focus_lost(pool).await,
                        Event::Key(key) => window.on_key(pool, key).await,
                        Event::Mouse(mouse) => window.on_mouse(pool, mouse).await,
                        Event::Paste(data) => window.on_paste(pool, data).await,
                        Event::Resize(columns, rows) => window.on_resize(pool, columns, rows).await,
                    }
                }
            },
        };
        Ok(())
    }

    fn get_active_window<'a>(&'a mut self) -> Option<&'a mut Windows> {
        if self.windows.len() <= self.active_window {
            None
        } else {
            Some(&mut self.windows[self.active_window])
        }
    }

    fn replace_active_window(&mut self, window: Windows) {
        if self.windows.len() <= self.active_window {
        } else {
            self.windows[self.active_window] = window;
        }
    }
}
