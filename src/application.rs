use crate::default_window::DefaultWindow;
use crate::log::Log;
use crossterm::event;
use crossterm::event::{poll, Event, KeyEvent, MouseEvent};
use ratatui::{DefaultTerminal, Frame};
use sqlx::{Pool, Sqlite};
use std::io;
use std::time::Duration;

pub trait Window {
    /// Renders the window
    fn render(self: &mut Self, app: AppContext, frame: &mut Frame);

    /// Performs an update step if needed
    async fn step(self: &mut Self, app: AppContext);

    /// The terminal gained focus
    async fn on_focus_gained(self: &mut Self, app: AppContext);

    /// The terminal lost focus
    async fn on_focus_lost(self: &mut Self, app: AppContext);

    /// A single key event with additional pressed modifiers.
    async fn on_key(self: &mut Self, app: AppContext, key: KeyEvent);

    /// A single mouse event with additional pressed modifiers.
    async fn on_mouse(self: &mut Self, app: AppContext, mouse: MouseEvent);

    /// A string that was pasted into the terminal. Only emitted if bracketed paste has been
    /// enabled.
    async fn on_paste(self: &mut Self, app: AppContext, data: String);

    /// A resize event with new dimensions after resize (columns, rows).
    /// **Note** that resize events can occur in batches.
    async fn on_resize(self: &mut Self, app: AppContext, columns: u16, rows: u16);
}
#[derive(Clone, Debug)]
pub enum Windows {
    Default(DefaultWindow),
}

impl Windows {
    fn render(self: &mut Self, app: AppContext, frame: &mut Frame) {
        match self {
            Windows::Default(w) => w.render(app, frame),
        }
    }
    async fn step(self: &mut Self, app: AppContext<'_>) {
        match self {
            Windows::Default(w) => w.step(app).await,
        }
    }
    async fn on_focus_gained(self: &mut Self, app: AppContext<'_>) {
        match self {
            Windows::Default(w) => w.on_focus_gained(app).await,
        }
    }
    async fn on_focus_lost(self: &mut Self, app: AppContext<'_>) {
        match self {
            Windows::Default(w) => w.on_focus_lost(app).await,
        }
    }
    async fn on_key(self: &mut Self, app: AppContext<'_>, key: KeyEvent) {
        match self {
            Windows::Default(w) => w.on_key(app, key).await,
        }
    }
    async fn on_mouse(self: &mut Self, app: AppContext<'_>, mouse: MouseEvent) {
        match self {
            Windows::Default(w) => w.on_mouse(app, mouse).await,
        }
    }
    async fn on_paste(self: &mut Self, app: AppContext<'_>, data: String) {
        match self {
            Windows::Default(w) => w.on_paste(app, data).await,
        }
    }
    async fn on_resize(self: &mut Self, app: AppContext<'_>, columns: u16, rows: u16) {
        match self {
            Windows::Default(w) => w.on_resize(app, columns, rows).await,
        }
    }
}
pub struct Application<'a> {
    exit: bool,
    active_window: usize,
    windows: Vec<Windows>,
    pool: Pool<Sqlite>,
    log: Log<'a>,
}

#[derive(Clone)]
pub struct AppContext<'a> {
    pub pool: Pool<Sqlite>,
    pub log: Log<'a>,
}

impl Application<'_> {
    pub fn context(&self) -> AppContext{
        let pool = self.pool.clone();
        let log = self.log.clone();
        AppContext { pool, log }
    }
}

impl<'me> Application<'me> {
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
            log: Log::create_new(),
        }
    }
    pub async fn application_loop(&mut self, mut terminal: DefaultTerminal) -> io::Result<()> {
        let app_context = self.context();
        while !self.exit {
            // Render
            terminal.draw(|frame| {
                if let Some(active_window) = self.windows.get_mut(self.active_window) {
                    active_window.render(app_context.clone(), frame);
                }
            })?;

            // Update
            for index in 0..self.windows.len() {
                let window = &mut self.windows[index];
                window.step(app_context.clone()).await;
            }

            // Events
            if poll(Duration::from_millis(1000 / 60))? {
                self.handle_events(event::read()?).await?;
            }
        }
        Ok(())
    }

    async fn handle_events(&mut self, event: Event) -> io::Result<()> {
        let app_context = self.context();
        let active_window = self.get_active_window();
        match active_window {
            None => {
                self.exit = true;
            }
            Some(window) => match event {
                Event::FocusGained => window.on_focus_gained(app_context).await,
                Event::FocusLost => window.on_focus_lost(app_context).await,
                Event::Key(key) => window.on_key(app_context, key).await,
                Event::Mouse(mouse) => window.on_mouse(app_context, mouse).await,
                Event::Paste(data) => window.on_paste(app_context, data).await,
                Event::Resize(columns, rows) => window.on_resize(app_context, columns, rows).await,
            },
        };
        Ok(())
    }

    fn get_active_window(&mut self) -> Option<&mut Windows> {
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
