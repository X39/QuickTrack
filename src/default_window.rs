use crate::application::{AppContext, Window};
use crate::command_handler::CommandHandler;
use crate::log::Message;
use crossterm::event::{Event, KeyCode, KeyEvent, KeyEventKind, MouseEvent};
use ratatui::prelude::Constraint::{Length, Min};
use ratatui::prelude::*;
use ratatui::prelude::{Layout, Stylize};
use ratatui::symbols::border;
use ratatui::text::Line;
use ratatui::widgets::{Block, Borders, List, ListItem, ListState, Padding, Paragraph};
use ratatui::Frame;
use std::cmp::max;
use ratatui::layout::Constraint::Max;
use tui_input::backend::crossterm::EventHandler;
use tui_input::Input;

#[derive(Debug, Clone)]
pub struct DefaultWindow {
    input: Input,
    log_state: ListState,
    command_handler: CommandHandler,
    project: String,
    location: String,
    undo_stack: Vec<String>,
    redo_stack: Vec<String>,
}

impl DefaultWindow {
    pub fn new() -> Self {
        Self {
            input: Input::default(),
            log_state: ListState::default(),
            command_handler: CommandHandler::new(),
            project: "".to_string(),
            location: "".to_string(),
            undo_stack: Vec::new(),
            redo_stack: Vec::new(),
        }
    }

    pub async fn update_project_from_db(self: &mut Self, app: &AppContext) {
        let project = crate::db_repository::get_active_project(app.pool.clone()).await;
        if let Err(e) = project {
            app.log.append(Message::Error(format!("Failed to update active project in view - SQLite Error: {:?}", e)));
        } else {
            let project = project.unwrap();
            if let Some(project) = project {
                self.project = project.title;
            } else {
                self.project = "".to_string();
            }
        }
    }
    pub async fn update_location_from_db(self: &mut Self, app: &AppContext) {
        let location = crate::db_repository::get_active_location(app.pool.clone()).await;
        if let Err(e) = location {
            app.log.append(Message::Error(format!("Failed to update active location in view - SQLite Error: {:?}", e)));
        } else {
            let location = location.unwrap();
            if let Some(location) = location {
                self.location = location.title;
            } else {
                self.location = "".to_string();
            }
        }
    }
}

impl Window for DefaultWindow {
    fn render(self: &mut Self, app: &AppContext, frame: &mut Frame) {
        let lines = max(self.input.value().lines().count(), 1);
        let layout = Layout::vertical([Min(0), Length(lines as u16 + 2)]);
        let [history_area, input_area] = layout.areas(frame.area());

        let messages = app.log.messages();

        let from = self.log_state.offset();
        let mut to = self.log_state.selected().unwrap_or(messages.len());
        if messages.len() > 0 && to != usize::MAX {
            to = to + 1;
        }
        let history = List::new(messages.iter().map(|msg| {
            ListItem::new(match msg {
                Message::Log(s) => s.clone().fg(Color::Gray).bg(Color::Black),
                Message::Normal(s) => s.clone().fg(Color::White).bg(Color::Black),
                Message::Info(s) => s.clone().fg(Color::LightBlue).bg(Color::Black),
                Message::Warning(s) => s.clone().fg(Color::LightYellow).bg(Color::Black),
                Message::Error(s) => s.clone().fg(Color::White).bg(Color::Red),
                Message::Success(s) => s.clone().fg(Color::Green).bg(Color::Black),
                Message::Failure(s) => s.clone().fg(Color::Red).bg(Color::Black),
            })
        }))
        .block(
            Block::bordered()
                .title_top("Output")
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
        let input_layout = Layout::horizontal([Length((self.location.len() + 2) as u16), Length((self.project.len() + 2) as u16), Min(0)]);
        let [location_area, project_area, user_input_area] = input_layout.areas(input_area);

        let text_input = Paragraph::new(self.location.as_str())
            .scroll((0, scroll as u16))
            .block(Block::bordered().border_set(border::THICK).title("Location"));
        frame.render_widget(text_input, location_area);

        let text_input = Paragraph::new(self.project.as_str())
            .scroll((0, scroll as u16))
            .block(Block::bordered().border_set(border::THICK).title("Project"));
        frame.render_widget(text_input, project_area);

        let text_input = Paragraph::new(self.input.value())
            .style(Style::default().fg(Color::Yellow))
            .scroll((0, scroll as u16))
            .block(Block::bordered().border_set(border::THICK).title("Input"));
        frame.render_widget(text_input, user_input_area);

        frame.set_cursor_position(Position::new(
            user_input_area.x + (self.input.visual_cursor().max(scroll) - scroll) as u16 + 1,
            user_input_area.y + 1,
        ));
    }
    async fn step(self: &mut Self, app: &AppContext) {}

    async fn on_focus_gained(self: &mut Self, app: &AppContext) {}

    async fn on_focus_lost(self: &mut Self, app: &AppContext) {}

    async fn on_key(self: &mut Self, app: &AppContext, key: KeyEvent) {
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
            },
            KeyCode::Up => {
                if key.kind == KeyEventKind::Press {
                    let undo_stack_len = self.undo_stack.len();
                    if undo_stack_len > 0 {
                        let line = self.input.value().to_string();
                        if line.len() > 0 {
                            self.redo_stack.push(line);
                        }
                        let line = self.undo_stack.pop().unwrap();
                        self.input = self.input.clone().with_value(line);
                    }
                }
            }
            KeyCode::Down => {
                if key.kind == KeyEventKind::Press {
                    let redo_stack_len = self.redo_stack.len();
                    if redo_stack_len > 0 {
                        let line = self.input.value().to_string();
                        if line.len() > 0 {
                            self.undo_stack.push(line);
                        }
                        let line = self.redo_stack.pop().unwrap();
                        self.input = self.input.clone().with_value(line);
                    }
                }
            }
            KeyCode::Enter => {
                if key.kind == KeyEventKind::Press {
                    let line = self.input.value().to_string();
                    if self
                        .command_handler
                        .handle_input(app, &line)
                        .await
                    {
                        self.input.reset();
                        for line in self.redo_stack.drain(0..) {
                            if line.len() == 0 {
                                continue;
                            }
                            self.undo_stack.push(line);
                        }
                        self.redo_stack.clear();
                        self.undo_stack.push(line);
                        self.update_project_from_db(app).await;
                        self.update_location_from_db(app).await;
                    }
                    self.log_state.select(Some(app.log.messages().len()));
                }
            }
            _ => {
                self.input.handle_event(&Event::Key(key));
            }
        };
    }

    async fn on_mouse(self: &mut Self, app: &AppContext, _mouse: MouseEvent) {}

    async fn on_paste(self: &mut Self, app: &AppContext, _data: String) {}

    async fn on_resize(self: &mut Self, app: &AppContext, _columns: u16, _rows: u16) {}
}
