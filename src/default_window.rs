use crate::application::Window;
use crate::command_handler::CommandHandler;
use crossterm::event::{Event, KeyCode, KeyEvent, MouseEvent};
use ratatui::prelude::Constraint::{Length, Min};
use ratatui::prelude::*;
use ratatui::prelude::{Layout, Stylize};
use ratatui::symbols::border;
use ratatui::text::Line;
use ratatui::widgets::{Block, List, ListItem, Paragraph};
use ratatui::Frame;
use sqlx::{Pool, Sqlite};
use std::cmp::max;
use tui_input::backend::crossterm::EventHandler;
use tui_input::Input;
/*
 * ToDo: Create a window as follows:
 *       ┌──────────────────────────────────────────────────────────┐
 *       │	                                                        │
 *       │	                                                        │
 *       │	                                                        │
 *       │	Log Messages                                            │
 *       ├──────────────────────────────────────────────────────────┤
 *       │	Project: Topic                                          │
 *       └──────────────────────────────────────────────────────────┘
 */

#[derive(Debug, Clone, Default)]
pub struct DefaultWindow<'a> {
    input: Input,
    command_handler: CommandHandler<'a>,
}

impl DefaultWindow<'_> {
    pub(crate) fn new() -> Self {
        Default::default()
    }
}

impl Window for DefaultWindow<'_> {
    fn render(self: &mut Self, frame: &mut Frame) {
        let lines = max(self.input.value().lines().count(), 1);
        let layout = Layout::vertical([Min(0), Length(lines as u16 + 2)]);
        let [history_area, input_area] = layout.areas(frame.area());

        let messages = self.command_handler.messages();
        let history = List::new(messages.iter().map(|span| ListItem::new(span.clone())))
            .block(
                Block::bordered()
                    .title(Line::from(" History ".bold()))
                    .border_set(border::THICK),
            );
        frame.render_widget(history, history_area);

        let scroll = self.input.visual_scroll(input_area.width as usize);
        let text_input = Paragraph::new(self.input.value())
            .style(Style::default().fg(Color::Yellow))
            .scroll((0, scroll as u16))
            .block(Block::bordered().border_set(border::THICK));
        frame.render_widget(text_input, input_area);

        frame.set_cursor_position(Position::new(
            input_area.x + (self.input.visual_cursor().max(scroll) - scroll) as u16 + 1,
            input_area.y + 1,
        ));
    }
    async fn step(self: &mut Self, _pool: Pool<Sqlite>) {}

    async fn on_focus_gained(self: &mut Self, _pool: Pool<Sqlite>) {}

    async fn on_focus_lost(self: &mut Self, _pool: Pool<Sqlite>) {}

    async fn on_key(self: &mut Self, pool: Pool<Sqlite>, key: KeyEvent) {
        match key.code {
            KeyCode::Enter => {
                let line = self.input.value();
                if self.command_handler.handle_input(pool, line.to_string()) {
                    self.input.reset();
                }
            }
            _ => {
                self.input.handle_event(&Event::Key(key));
            }
        };
    }

    async fn on_mouse(self: &mut Self, _pool: Pool<Sqlite>, _mouse: MouseEvent) {}

    async fn on_paste(self: &mut Self, _pool: Pool<Sqlite>, _data: String) {}

    async fn on_resize(self: &mut Self, _pool: Pool<Sqlite>, _columns: u16, _rows: u16) {}
}
