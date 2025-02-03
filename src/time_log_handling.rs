use crate::db_data::{Location, Project, TimeLog};
use sqlx::{Pool, Sqlite};

pub struct TimeLogExtended {
    pub time_log: TimeLog,
    pub timestamp_finished: Option<chrono::DateTime<chrono::Utc>>,
}

impl TimeLogExtended {
    pub(crate) async fn get_locations(
        pool: Pool<Sqlite>,
        iter: impl Iterator<Item = &TimeLog>,
    ) -> Result<Vec<Location>, sqlx::Error> {
        let mut locations: Vec<Location> = vec![];
        for time_log in iter {
            if locations
                .iter()
                .find(|location| location.id == time_log.location_id)
                .is_some()
            {
                continue;
            }
            let location =
                crate::db_repository::get_location_by_id(pool.clone(), time_log.location_id)
                    .await?;
            if let Some(location) = location {
                locations.push(location);
            }
        }
        Ok(locations)
    }
    pub(crate) async fn get_projects(
        pool: Pool<Sqlite>,
        iter: impl Iterator<Item = &TimeLog>,
    ) -> Result<Vec<Project>, sqlx::Error> {
        let mut projects: Vec<Project> = vec![];
        for time_log in iter {
            if projects
                .iter()
                .find(|project| project.id == time_log.project_id)
                .is_some()
            {
                continue;
            }
            let project =
                crate::db_repository::get_project_by_id(pool.clone(), time_log.project_id).await?;
            if let Some(project) = project {
                projects.push(project);
            }
        }
        Ok(projects)
    }
    pub fn handle_logs<'a>(
        iter: impl Iterator<Item = &'a TimeLog>,
        mut fnc: impl FnMut(TimeLogExtended),
    ) {
        let mut time_logs = vec![];
        let mut previous_time_log_opt: Option<&TimeLog> = None;
        for time_log in iter {
            if let Some(previous_time_log) = previous_time_log_opt {
                time_logs.push(TimeLogExtended {
                    time_log: previous_time_log.clone(),
                    timestamp_finished: Some(time_log.timestamp_created),
                });
                previous_time_log_opt = Some(&time_log);
            } else {
                previous_time_log_opt = Some(&time_log);
            }
        }
        if let Some(previous_time_log) = previous_time_log_opt {
            time_logs.push(TimeLogExtended {
                time_log: previous_time_log.clone(),
                timestamp_finished: None,
            });
        }
        for time_log in time_logs {
            fnc(time_log);
        }
    }
}
