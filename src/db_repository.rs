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

pub(crate) async fn get_or_add_project(
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
        active: project.active,
    })
}
pub(crate) async fn get_project(
    pool: Pool<Sqlite>,
    project: &String,
) -> Result<Option<Project>, sqlx::Error> {
    let projects = query!("SELECT * FROM projects WHERE title = ?;", project);
    let project = projects.fetch_optional(&pool).await?;
    if let Some(project) = project {
        Ok(Some(Project {
            id: project.id,
            title: project.title,
            timestamp_created: to_date_time_utc(project.timestamp_created),
            active: project.active,
        }))
    } else {
        Ok(None)
    }
}
pub(crate) async fn get_project_by_id(
    pool: Pool<Sqlite>,
    project_id: i64,
) -> Result<Option<Project>, sqlx::Error> {
    let projects = query!("SELECT * FROM projects WHERE id = ?;", project_id);
    let project = projects.fetch_optional(&pool).await?;
    if let Some(project) = project {
        Ok(Some(Project {
            id: project.id,
            title: project.title,
            timestamp_created: to_date_time_utc(project.timestamp_created),
            active: project.active,
        }))
    } else {
        Ok(None)
    }
}

pub(crate) async fn get_active_project(pool: Pool<Sqlite>) -> Result<Option<Project>, sqlx::Error> {
    let projects = query!("SELECT * FROM projects WHERE active = TRUE;");
    let project = projects.fetch_optional(&pool).await?;

    if let Some(project) = project {
        Ok(Some(Project {
            id: project.id,
            title: project.title,
            timestamp_created: to_date_time_utc(project.timestamp_created),
            active: project.active,
        }))
    } else {
        Ok(None)
    }
}
pub(crate) async fn get_or_add_location(
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
        active: location.active,
    })
}
pub(crate) async fn get_location(
    pool: Pool<Sqlite>,
    location: &String,
) -> Result<Option<Location>, sqlx::Error> {
    let locations = query!("SELECT * FROM locations WHERE title = ?;", location);
    let location = locations.fetch_optional(&pool).await?;
    if let Some(location) = location {
        Ok(Some(Location {
            id: location.id,
            title: location.title,
            timestamp_created: to_date_time_utc(location.timestamp_created),
            active: location.active,
        }))
    } else {
        Ok(None)
    }
}
pub(crate) async fn get_location_by_id(
    pool: Pool<Sqlite>,
    location_id: i64,
) -> Result<Option<Location>, sqlx::Error> {
    let locations = query!("SELECT * FROM locations WHERE id = ?;", location_id);
    let location = locations.fetch_optional(&pool).await?;
    if let Some(location) = location {
        Ok(Some(Location {
            id: location.id,
            title: location.title,
            timestamp_created: to_date_time_utc(location.timestamp_created),
            active: location.active,
        }))
    } else {
        Ok(None)
    }
}
pub(crate) async fn get_active_location(
    pool: Pool<Sqlite>,
) -> Result<Option<Location>, sqlx::Error> {
    let locations = query!("SELECT * FROM locations WHERE active = TRUE;");
    let location = locations.fetch_optional(&pool).await?;

    if let Some(location) = location {
        Ok(Some(Location {
            id: location.id,
            title: location.title,
            timestamp_created: to_date_time_utc(location.timestamp_created),
            active: location.active,
        }))
    } else {
        Ok(None)
    }
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
    let day = get_day(
        pool.clone(),
        ts.year() as u16,
        ts.month() as u16,
        ts.day() as u16,
    )
    .await?;
    let project = get_or_add_project(pool.clone(), project).await?;
    let location = get_or_add_location(pool.clone(), location).await?;
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
    let row = query!(
        "SELECT * FROM time_log WHERE deleted = FALSE AND id = ?",
        row
    )
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

pub async fn drop_time_log(pool: Pool<Sqlite>, time_log_id: i64) -> Result<(), sqlx::Error> {
    let ts = Utc::now();
    query_scalar!(
        "INSERT INTO time_log_audit (timestamp_created, kind, message) VALUES (?, ?, 'Marked time-log as deleted');
         UPDATE time_log SET deleted = TRUE WHERE id = ?;", ts, TimeLogKind::LogLineMarkedAsDeleted as u8, time_log_id)
        .execute(&pool).await?;
    Ok(())
}

pub(crate) async fn set_active_project(
    pool: Pool<Sqlite>,
    project: &String,
) -> Result<(), sqlx::Error> {
    query!("UPDATE projects SET active = FALSE WHERE active = TRUE;")
        .execute(&pool)
        .await?;

    query!(
        "UPDATE projects SET active = TRUE WHERE title = ?;",
        project
    )
    .execute(&pool)
    .await?;
    Ok(())
}

pub(crate) async fn set_active_location(
    pool: Pool<Sqlite>,
    location: &String,
) -> Result<(), sqlx::Error> {
    query!("UPDATE locations SET active = FALSE WHERE active = TRUE;")
        .execute(&pool)
        .await?;

    query!(
        "UPDATE locations SET active = TRUE WHERE title = ?;",
        location
    )
    .execute(&pool)
    .await?;
    Ok(())
}

pub(crate) async fn get_n_days(
    pool: Pool<Sqlite>,
    skip: i64,
    limit: i64,
) -> Result<Vec<Day>, sqlx::Error> {
    let rows = query!(
        "SELECT * FROM days ORDER BY year DESC, month DESC, day DESC LIMIT ? OFFSET ?;",
        limit,
        skip
    )
    .fetch_all(&pool)
    .await?;

    let mut days: Vec<Day> = Vec::new();
    for row in rows {
        days.push(Day {
            id: row.id,
            day: row.day as u16,
            month: row.month as u16,
            year: row.year as u16,
        })
    }
    days.reverse();
    Ok(days)
}

pub(crate) async fn get_time_logs_of_day(
    pool: Pool<Sqlite>,
    day_id: i64,
) -> Result<Vec<TimeLog>, sqlx::Error> {
    let rows = query!(
        "SELECT * FROM time_log WHERE deleted = FALSE AND day_fk = ?;",
        day_id
    )
    .fetch_all(&pool)
    .await?;

    let mut time_logs = vec![];
    for row in rows {
        time_logs.push(TimeLog {
            id: row.id,
            timestamp_created: to_date_time_utc(row.timestamp_created),
            message: row.message,
            day_id: row.day_fk,
            mode: TimeLogMode::from(row.mode),
            project_id: row.project_fk,
            location_id: row.location_fk,
        })
    }
    Ok(time_logs)
}

pub async fn get_n_time_logs_id_desc(
    pool: Pool<Sqlite>,
    skip: i64,
    take: i64,
) -> Result<Vec<TimeLog>, sqlx::Error> {
    let rows = query!(
        "SELECT * FROM time_log ORDER BY id DESC LIMIT ? OFFSET ?;",
        take,
        skip
    )
    .fetch_all(&pool)
    .await?;

    let mut time_logs = vec![];
    for row in rows {
        time_logs.push(TimeLog {
            id: row.id,
            timestamp_created: to_date_time_utc(row.timestamp_created),
            message: row.message,
            day_id: row.day_fk,
            mode: TimeLogMode::from(row.mode),
            project_id: row.project_fk,
            location_id: row.location_fk,
        });
    }
    Ok(time_logs)
}
