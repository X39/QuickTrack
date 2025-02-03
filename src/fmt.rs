use chrono::TimeDelta;

pub trait Formatter {
    fn default_format(&self) -> String;
}
impl Formatter for TimeDelta {
    fn default_format(&self) -> String {
        format!(
            "{:0>2}:{:0>2}:{:0>2}",
            self.num_hours(),
            self.num_minutes() % 60,
            self.num_seconds() % 60
        )
    }
}
