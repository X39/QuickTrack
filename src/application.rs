use std::io;
use std::time::Duration;
use crossterm::event;
use crossterm::event::{poll, Event, KeyCode, KeyEvent, KeyEventKind, MouseEvent};
use ratatui::{DefaultTerminal, Frame};
use ratatui::prelude::Stylize;
use ratatui::widgets::Paragraph;
use crate::application::MutateResult::Changed;

pub enum MutateResult {
    Unchanged,
    Changed(Box<dyn Window>),
}

pub trait Window {
    /// Renders the window
    fn render(self: &Self, frame: Frame);

    /// Performs an update step if needed
    fn step(self: &Self) -> MutateResult;

    /// The terminal gained focus
    fn on_focus_gained(self: &Self) -> MutateResult;

    /// The terminal lost focus
    fn on_focus_lost(self: &Self) -> MutateResult;

    /// A single key event with additional pressed modifiers.
    fn on_key(self: &Self, key: KeyEvent) -> MutateResult;

    /// A single mouse event with additional pressed modifiers.
    fn on_mouse(self: &Self, mouse: MouseEvent) -> MutateResult;

    /// A string that was pasted into the terminal. Only emitted if bracketed paste has been
    /// enabled.
    fn on_paste(self: &Self, data: String) -> MutateResult;

    /// A resize event with new dimensions after resize (columns, rows).
    /// **Note** that resize events can occur in batches.
    fn on_resize(self: &Self, columns: u16, rows: u16) -> MutateResult;
}
pub struct Application {
    exit: bool,
    active_window: usize,
    windows: Vec<Box<dyn Window>>,
}

impl Application {
    pub fn push_window(&mut self, window: Box<dyn Window>) {
        let index = self.windows.len();
        self.windows.push(window);
        self.active_window = index;
    }

    pub fn new() -> Self {
        Application {
            exit: false,
            active_window: 0,
            windows: Vec::new(),
        }
    }
    pub fn application_loop(&mut self, mut terminal: DefaultTerminal) -> io::Result<()> {
        while !self.exit {
            // Render
            terminal.draw(|frame| {
                self.render(frame);
            })?;

            // Update
            for index in 0..self.windows.len() {
                let window = &self.windows[index];
                if let Changed(new_window) = window.step() {
                    self.windows[index] = new_window;
                }
            }

            // Events
            if poll(Duration::from_millis(1000 / 60))? {
                self.handle_events(event::read()?)?;
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

    fn handle_events(&mut self, event: Event) -> io::Result<()> {
        let active_window = self.get_active_window();
        let mutation = match active_window {
            None => { self.exit = true; MutateResult::Unchanged }
            Some(window) => match event {
                Event::FocusGained => window.on_focus_gained(),
                Event::FocusLost => window.on_focus_lost(),
                Event::Key(key) => window.on_key(key),
                Event::Mouse(mouse) => window.on_mouse(mouse),
                Event::Paste(data) => window.on_paste(data),
                Event::Resize(columns, rows) => window.on_resize(columns, rows),
            }
        };
        if let Changed(new_active_window) = mutation {
            self.replace_active_window(new_active_window);
        }
        Ok(())
    }

    fn get_active_window<'a>(&'a self) -> Option<&'a Box<dyn Window>> {
        if self.windows.len() <= self.active_window {
            None
        } else {
            Some(&self.windows[self.active_window])
        }
    }

    fn replace_active_window(&mut self, window: Box<dyn Window>) {
        if self.windows.len() <= self.active_window {
        } else {
            self.windows[self.active_window] = window;
        }
    }
}
