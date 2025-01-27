use crate::application::{AppContext, Application, Window};
use crate::command_handler::CommandHandler;
use crossterm::event::{Event, KeyCode, KeyEvent, KeyEventKind, KeyEventState, MouseEvent};
use ratatui::prelude::Constraint::{Length, Min};
use ratatui::prelude::*;
use ratatui::prelude::{Layout, Stylize};
use ratatui::symbols::border;
use ratatui::text::Line;
use ratatui::widgets::{Block, List, ListItem, ListState, Paragraph};
use ratatui::Frame;
use sqlx::{Pool, Sqlite};
use std::cmp::{max, min};
use tui_input::backend::crossterm::EventHandler;
use tui_input::Input;

#[derive(Debug, Clone, Default)]
pub struct DefaultWindow {
    input: Input,
    log_state: ListState,
    command_handler: CommandHandler,
}

impl DefaultWindow {
    pub(crate) fn new() -> Self {
        Default::default()
    }
}

impl Window for DefaultWindow {
    fn render(self: &mut Self, app: AppContext, frame: &mut Frame) {
        let lines = max(self.input.value().lines().count(), 1);
        let layout = Layout::vertical([Min(0), Length(lines as u16 + 2)]);
        let [history_area, input_area] = layout.areas(frame.area());

        let messages = app.log.messages();

        let from = self.log_state.offset();
        let mut to = self.log_state.selected().unwrap_or(messages.len());
        if messages.len() > 0 && to != usize::MAX {
            to = to + 1;
        }
        let history = List::new(messages.iter().map(|span| ListItem::new(span.clone())))
            .block(
                Block::bordered()
                    .title_bottom(Line::from(
                        if messages.len() < 10 {
                            format!(
                                " {: >1} - {: >1} / {: >1} | PG-UP/PG-DOWN to scroll ",
                                from,
                                to,
                                messages.len()
                            )
                        } else if messages.len() < 100 {
                            format!(
                                " {: >2} - {: >2} / {: >2} | PG-UP/PG-DOWN to scroll ",
                                from,
                                to,
                                messages.len()
                            )
                        } else if messages.len() < 1000 {
                            format!(
                                " {: >3} - {: >3} / {: >3} | PG-UP/PG-DOWN to scroll ",
                                from,
                                to,
                                messages.len()
                            )
                        } else if messages.len() < 10000 {
                            format!(
                                " {: >4} - {: >4} / {: >4} | PG-UP/PG-DOWN to scroll ",
                                from,
                                to,
                                messages.len()
                            )
                        } else {
                            format!(
                                " {: >5} - {: >5} / {: >5} | PG-UP/PG-DOWN to scroll ",
                                from,
                                to,
                                messages.len()
                            )
                        }
                        .bold(),
                    ))
                    .title_alignment(Alignment::Center)
                    .border_set(border::THICK),
            )
            .highlight_symbol("> ");

        frame.render_stateful_widget(history, history_area, &mut self.log_state);

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
    async fn step(self: &mut Self, app: AppContext<'_>) {}

    async fn on_focus_gained(self: &mut Self, app: AppContext<'_>) {}

    async fn on_focus_lost(self: &mut Self, app: AppContext<'_>) {}

    async fn on_key(self: &mut Self, app: AppContext<'_>, key: KeyEvent) {
        match key.code {
            KeyCode::PageUp => {
                if key.kind == KeyEventKind::Press {
                    self.log_state.select_previous();
                }
            }
            KeyCode::PageDown => {
                if key.kind == KeyEventKind::Press {
                    self.log_state.select_next();
                }
            }
            KeyCode::Enter => {
                if key.kind == KeyEventKind::Press {
                    let line = self.input.value();
                    if self
                        .command_handler
                        .handle_input(app.clone(), line.to_string())
                        .await
                    {
                        self.input.reset();
                    }
                    self.log_state.select(Some(app.log.messages().len()));
                }
            }
            _ => {
                self.input.handle_event(&Event::Key(key));
            }
        };
    }

    async fn on_mouse(self: &mut Self, app: AppContext<'_>, _mouse: MouseEvent) {}

    async fn on_paste(self: &mut Self, app: AppContext<'_>, _data: String) {}

    async fn on_resize(self: &mut Self, app: AppContext<'_>, _columns: u16, _rows: u16) {}
}
