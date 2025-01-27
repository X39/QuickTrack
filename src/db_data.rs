use chrono::{Datelike, Timelike, Weekday};

pub struct Day {
    pub id: i64,
    pub year: u16,
    pub month: u16,
    pub day: u16,
}
pub struct Project {
    pub id: i64,
    pub title: String,
    pub timestamp_created: chrono::DateTime<chrono::Utc>,
}
pub struct Location {
    pub id: i64,
    pub title: String,
    pub timestamp_created: chrono::DateTime<chrono::Utc>,
}
#[derive(Copy, Clone, Debug, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub enum TimeLogKind {
    Note = 0,
    DayCreated = 1,
    LogLineAppended = 2,
    LogLineUpdated = 3,
    LogLineRemoved = 4,
}
pub struct TimeLogAudit {
    pub id: i64,
    pub timestamp_created: chrono::DateTime<chrono::Utc>,
    pub kind: TimeLogKind,
    pub message: String,
}

#[derive(Copy, Clone, Debug, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub enum TimeLogMode {
    /// # Summary
    /// Default time mode. Indicates normal, counted work.
    Normal = 0,
    /// # Summary
    /// Time mode for break. Indicates a non-counted piece of time.
    Break = 1,
    /// # Summary
    /// Day termination. Used to indicate an end.
    Quit = 2,
    /// # Summary
    /// Specialized time mode to denote that automated export occured during this time frame.
    Export = 3,
    /// # Summary
    /// Similar to <see cref="Break"/> but the time is counted.
    ///
    /// # Remarks
    /// Supposed to be used when eg. Half-Day off is needed.
    OffTime = 4,
}
impl From<i64> for TimeLogMode {
    fn from(value: i64) -> Self {
        match value {
            0 => TimeLogMode::Normal,
            1 => TimeLogMode::Break,
            2 => TimeLogMode::Quit,
            3 => TimeLogMode::Export,
            4 => TimeLogMode::OffTime,
            _ => panic!("Invalid TimeLogMode value {:?}", value),
        }
    }
}

pub struct TimeLog {
    pub id: i64,
    pub day_id: i64,
    pub project_id: i64,
    pub location_id: i64,
    pub timestamp_created: chrono::DateTime<chrono::Utc>,
    pub message: String,
    pub mode: TimeLogMode,
}

impl TimeLog {
    pub fn to_display_string(
        &self,
        next_ts: Option<chrono::DateTime<chrono::Utc>>,
        location: Option<&String>,
        project: Option<&String>,
    ) -> String {
        let weekday = match self.timestamp_created.weekday() {
            Weekday::Mon => "MON",
            Weekday::Tue => "TUE",
            Weekday::Wed => "WED",
            Weekday::Thu => "THU",
            Weekday::Fri => "FRI",
            Weekday::Sat => "SAT",
            Weekday::Sun => "SUN",
        };
        match next_ts {
            None => match location {
                None => match project {
                    None => format!(
                        "[{:0>4}-{:0>2}-{:>2}][{}][{:0>2}:{:0>2}] {}",
                        self.timestamp_created.year(),
                        self.timestamp_created.month(),
                        self.timestamp_created.day(),
                        weekday,
                        self.timestamp_created.hour(),
                        self.timestamp_created.minute(),
                        self.message
                    ),
                    Some(project) => format!(
                        "[{:0>4}-{:0>2}-{:>2}][{}][{:0>2}:{:0>2}] {}: {}",
                        self.timestamp_created.year(),
                        self.timestamp_created.month(),
                        self.timestamp_created.day(),
                        weekday,
                        self.timestamp_created.hour(),
                        self.timestamp_created.minute(),
                        project,
                        self.message
                    ),
                },
                Some(location) => match project {
                    None => format!(
                        "[{:0>4}-{:0>2}-{:>2}][{}][{:0>2}:{:0>2}][{}] {}",
                        self.timestamp_created.year(),
                        self.timestamp_created.month(),
                        self.timestamp_created.day(),
                        weekday,
                        self.timestamp_created.hour(),
                        self.timestamp_created.minute(),
                        location,
                        self.message
                    ),
                    Some(project) => format!(
                        "[{:0>4}-{:0>2}-{:>2}][{}][{:0>2}:{:0>2}][{}] {}: {}",
                        self.timestamp_created.year(),
                        self.timestamp_created.month(),
                        self.timestamp_created.day(),
                        weekday,
                        self.timestamp_created.hour(),
                        self.timestamp_created.minute(),
                        location,
                        project,
                        self.message
                    ),
                },
            },
            Some(next_ts) => match location {
                None => match project {
                    None => format!(
                        "[{:0>4}-{:0>2}-{:>2}][{}][{:0>2}:{:0>2} - {:0>2}:{:0>2}] {}",
                        self.timestamp_created.year(),
                        self.timestamp_created.month(),
                        self.timestamp_created.day(),
                        weekday,
                        self.timestamp_created.hour(),
                        self.timestamp_created.minute(),
                        next_ts.hour(),
                        next_ts.minute(),
                        self.message
                    ),
                    Some(project) => format!(
                        "[{:0>4}-{:0>2}-{:>2}][{}][{:0>2}:{:0>2} - {:0>2}:{:0>2}] {}: {}",
                        self.timestamp_created.year(),
                        self.timestamp_created.month(),
                        self.timestamp_created.day(),
                        weekday,
                        self.timestamp_created.hour(),
                        self.timestamp_created.minute(),
                        next_ts.hour(),
                        next_ts.minute(),
                        project,
                        self.message
                    ),
                },
                Some(location) => match project {
                    None => format!(
                        "[{:0>4}-{:0>2}-{:>2}][{}][{:0>2}:{:0>2} - {:0>2}:{:0>2}][{}] {}",
                        self.timestamp_created.year(),
                        self.timestamp_created.month(),
                        self.timestamp_created.day(),
                        weekday,
                        self.timestamp_created.hour(),
                        self.timestamp_created.minute(),
                        next_ts.hour(),
                        next_ts.minute(),
                        location,
                        self.message
                    ),
                    Some(project) => format!(
                        "[{:0>4}-{:0>2}-{:>2}][{}][{:0>2}:{:0>2} - {:0>2}:{:0>2}][{}] {}: {}",
                        self.timestamp_created.year(),
                        self.timestamp_created.month(),
                        self.timestamp_created.day(),
                        weekday,
                        self.timestamp_created.hour(),
                        self.timestamp_created.minute(),
                        next_ts.hour(),
                        next_ts.minute(),
                        location,
                        project,
                        self.message
                    ),
                },
            },
        }
    }
}
