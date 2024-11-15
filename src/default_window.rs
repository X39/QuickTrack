use crossterm::event::{KeyEvent, MouseEvent};
use ratatui::Frame;
use crate::application::{MutateResult, Window};
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


#[derive(Debug, Clone, PartialEq, PartialOrd, Default)]
pub struct DefaultWindow {}

impl Window for DefaultWindow {
    fn render(self: &Self, frame: Frame) {
        todo!()
    }

    fn step(self: &Self) -> MutateResult {
        todo!()
    }

    fn on_focus_gained(self: &Self) -> MutateResult {
        todo!()
    }

    fn on_focus_lost(self: &Self) -> MutateResult {
        todo!()
    }

    fn on_key(self: &Self, key: KeyEvent) -> MutateResult {
        todo!()
    }

    fn on_mouse(self: &Self, mouse: MouseEvent) -> MutateResult {
        todo!()
    }

    fn on_paste(self: &Self, data: String) -> MutateResult {
        todo!()
    }

    fn on_resize(self: &Self, columns: u16, rows: u16) -> MutateResult {
        todo!()
    }
}