use chrono::{DateTime, Datelike, Local, TimeZone, Timelike, Weekday};

pub struct Day {
    pub id: i64,
    pub year: u16,
    pub month: u16,
    pub day: u16,
}

impl Day {
    pub(crate) fn from_date<Tz: TimeZone>(arg: &DateTime<Tz>) -> Day {
        Day {
            id: 0,
            year: arg.year() as u16,
            month: arg.month() as u16,
            day: arg.day() as u16,
        }
    }
}

pub struct Project {
    pub id: i64,
    pub title: String,
    pub timestamp_created: chrono::DateTime<chrono::Utc>,
    pub active: bool,
}
pub struct Location {
    pub id: i64,
    pub title: String,
    pub timestamp_created: chrono::DateTime<chrono::Utc>,
    pub active: bool,
}
#[derive(Copy, Clone, Debug, PartialEq, Eq, PartialOrd, Ord, Hash)]
pub enum TimeLogKind {
    Note = 0,
    DayCreated = 1,
    LogLineAppended = 2,
    LogLineUpdated = 3,
    LogLineMarkedAsDeleted = 4,
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

#[derive(Clone, Debug)]
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
    pub fn to_display_string_alt(
        &self,
        next_ts: Option<chrono::DateTime<chrono::Utc>>,
        location: Option<&Location>,
        project: Option<&Project>,
    ) -> String {
        match location {
            Some(location) => match project {
                Some(project) => {
                    self.to_display_string(next_ts, Some(&location.title), Some(&project.title))
                }
                None => self.to_display_string(next_ts, Some(&location.title), None),
            },
            None => match project {
                Some(project) => self.to_display_string(next_ts, None, Some(&project.title)),
                None => self.to_display_string(next_ts, None, None),
            },
        }
    }
    pub fn to_display_string(
        &self,
        next_ts: Option<chrono::DateTime<chrono::Utc>>,
        location: Option<&String>,
        project: Option<&String>,
    ) -> String {
        let local_time = self.timestamp_created.with_timezone(&chrono::Local);
        let weekday = match local_time.weekday() {
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
                        "[{:0>4}-{:0>2}-{:0>2}][{}][{:0>2}:{:0>2}] {}",
                        local_time.year(),
                        local_time.month(),
                        local_time.day(),
                        weekday,
                        local_time.hour(),
                        local_time.minute(),
                        self.message
                    ),
                    Some(project) => format!(
                        "[{:0>4}-{:0>2}-{:0>2}][{}][{:0>2}:{:0>2}] {}: {}",
                        local_time.year(),
                        local_time.month(),
                        local_time.day(),
                        weekday,
                        local_time.hour(),
                        local_time.minute(),
                        project,
                        self.message
                    ),
                },
                Some(location) => match project {
                    None => format!(
                        "[{:0>4}-{:0>2}-{:0>2}][{}][{:0>2}:{:0>2}][{}] {}",
                        local_time.year(),
                        local_time.month(),
                        local_time.day(),
                        weekday,
                        local_time.hour(),
                        local_time.minute(),
                        location,
                        self.message
                    ),
                    Some(project) => format!(
                        "[{:0>4}-{:0>2}-{:0>2}][{}][{:0>2}:{:0>2}][{}] {}: {}",
                        local_time.year(),
                        local_time.month(),
                        local_time.day(),
                        weekday,
                        local_time.hour(),
                        local_time.minute(),
                        location,
                        project,
                        self.message
                    ),
                },
            },
            Some(next_ts) => {
                let next_ts = next_ts.with_timezone(&chrono::Local);
                match location {
                    None => match project {
                        None => format!(
                            "[{:0>4}-{:0>2}-{:0>2}][{}][{:0>2}:{:0>2} - {:0>2}:{:0>2}] {}",
                            local_time.year(),
                            local_time.month(),
                            local_time.day(),
                            weekday,
                            local_time.hour(),
                            local_time.minute(),
                            next_ts.hour(),
                            next_ts.minute(),
                            self.message
                        ),
                        Some(project) => format!(
                            "[{:0>4}-{:0>2}-{:0>2}][{}][{:0>2}:{:0>2} - {:0>2}:{:0>2}] {}: {}",
                            local_time.year(),
                            local_time.month(),
                            local_time.day(),
                            weekday,
                            local_time.hour(),
                            local_time.minute(),
                            next_ts.hour(),
                            next_ts.minute(),
                            project,
                            self.message
                        ),
                    },
                    Some(location) => match project {
                        None => format!(
                            "[{:0>4}-{:0>2}-{:0>2}][{}][{:0>2}:{:0>2} - {:0>2}:{:0>2}][{}] {}",
                            local_time.year(),
                            local_time.month(),
                            local_time.day(),
                            weekday,
                            local_time.hour(),
                            local_time.minute(),
                            next_ts.hour(),
                            next_ts.minute(),
                            location,
                            self.message
                        ),
                        Some(project) => format!(
                            "[{:0>4}-{:0>2}-{:0>2}][{}][{:0>2}:{:0>2} - {:0>2}:{:0>2}][{}] {}: {}",
                            local_time.year(),
                            local_time.month(),
                            local_time.day(),
                            weekday,
                            local_time.hour(),
                            local_time.minute(),
                            next_ts.hour(),
                            next_ts.minute(),
                            location,
                            project,
                            self.message
                        ),
                    },
                }
            }
        }
    }
}
