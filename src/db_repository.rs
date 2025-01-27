use crate::db_data::{Day, Location, Project, TimeLog, TimeLogKind, TimeLogMode};
use chrono::{DateTime, Datelike, NaiveDateTime, Timelike, Utc};
use sqlx::{query, query_scalar, Pool, Sqlite};

fn to_date_time_utc(src: NaiveDateTime) -> DateTime<Utc> {
    let ts = src.and_utc();
    let ts = DateTime::<Utc>::from_timestamp(ts.timestamp(), ts.nanosecond());
    match ts {
        None => panic!("Failed to convert timestamp {:?} to DateTime<Utc>", src),
        Some(ts) => ts,
    }
}

pub(crate) async fn get_project(
    pool: Pool<Sqlite>,
    project: &String,
) -> Result<Project, sqlx::Error> {
    let projects = query!(
        "INSERT OR IGNORE INTO projects (title) VALUES (?);
         SELECT * FROM projects WHERE title = ?;
       ",
        project,
        project
    );
    let project = projects.fetch_one(&pool).await?;

    Ok(Project {
        id: project.id,
        title: project.title,
        timestamp_created: to_date_time_utc(project.timestamp_created),
    })
}
pub(crate) async fn get_location(
    pool: Pool<Sqlite>,
    location: &String,
) -> Result<Location, sqlx::Error> {
    let locations = query!(
        "INSERT OR IGNORE INTO locations (title) VALUES (?);
         SELECT * FROM locations WHERE title = ?;
       ",
        location,
        location
    );
    let location = locations.fetch_one(&pool).await?;

    Ok(Location {
        id: location.id,
        title: location.title,
        timestamp_created: to_date_time_utc(location.timestamp_created),
    })
}
pub(crate) async fn get_day(
    pool: Pool<Sqlite>,
    year: u16,
    month: u16,
    day: u16,
) -> Result<Day, sqlx::Error> {
    let days = query!(
        "INSERT OR IGNORE INTO days (day, month, year) VALUES (?, ?, ?);
         SELECT * FROM days WHERE day = ? AND month = ? AND year = ?;
       ",
        day,
        month,
        year,
        day,
        month,
        year
    );
    let day = days.fetch_one(&pool).await?;

    Ok(Day {
        id: day.id,
        day: day.day as u16,
        month: day.month as u16,
        year: day.year as u16,
    })
}

pub(crate) async fn add_time_log(
    pool: Pool<Sqlite>,
    project: &String,
    location: &String,
    mode: TimeLogMode,
    message: &String,
) -> Result<TimeLog, sqlx::Error> {
    let ts = Utc::now();
    let tl = TimeLog {
        timestamp_created: ts,
        message: message.to_string(),
        mode,

        id: 0,
        day_id: 0,
        project_id: 0,
        location_id: 0,
    };
    let tl = tl.to_display_string(None, Some(location), Some(project));
    let day = get_day(pool.clone(), ts.year() as u16, ts.month() as u16, ts.day() as u16).await?;
    let project = get_project(pool.clone(), project).await?;
    let location = get_location(pool.clone(), location).await?;
    let mode = mode as u8;
    let audit_kind = TimeLogKind::LogLineAppended as u8;
    let row = query_scalar!(
        "INSERT INTO time_log_audit (timestamp_created, kind, message) VALUES (?, ?, ?);\
        INSERT INTO time_log (day_fk, project_fk, location_fk, timestamp_created, message, mode)\
         VALUES (?, ?, ?, ?, ?, ?) RETURNING id;",
        ts,
        audit_kind,
        tl,
        day.id,
        project.id,
        location.id,
        ts,
        message,
        mode
    )
    .fetch_one(&pool)
    .await?;
    let row = query!("SELECT * FROM time_log WHERE id = ?", row)
        .fetch_one(&pool)
        .await?;
    Ok(TimeLog {
        id: row.id,
        timestamp_created: to_date_time_utc(row.timestamp_created),
        message: row.message,
        day_id: row.day_fk,
        mode: TimeLogMode::from(row.mode),
        project_id: row.project_fk,
        location_id: row.location_fk,
    })
}
