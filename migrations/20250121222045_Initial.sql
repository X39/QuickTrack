CREATE TABLE days
(
    id    INTEGER NOT NULL
        CONSTRAINT days_pk
            PRIMARY KEY AUTOINCREMENT,
    day   INTEGER NOT NULL,
    month INTEGER NOT NULL,
    year  INTEGER NOT NULL
);

CREATE UNIQUE INDEX days_year_month_day_uindex
    ON days (year, month, day);

CREATE TABLE projects
(
    id                INTEGER                            NOT NULL
        CONSTRAINT projects_pk
            PRIMARY KEY AUTOINCREMENT,
    title             TEXT                               NOT NULL,
    timestamp_created DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL
);

create unique index projects_title_uindex
    on projects (title);

CREATE TABLE locations
(
    id                INTEGER                            NOT NULL
        CONSTRAINT locations_pk
            PRIMARY KEY AUTOINCREMENT,
    title             TEXT                               NOT NULL,
    timestamp_created DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL
);

create unique index locations_title_uindex
    on locations (title);


CREATE TABLE time_log_audit
(
    id                INTEGER                            NOT NULL
        CONSTRAINT time_log_audit_pk
            PRIMARY KEY AUTOINCREMENT,
    timestamp_created DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    kind              INTEGER                            NOT NULL,
    message           TEXT                               NOT NULL
);

CREATE TABLE time_log
(
    id                INTEGER                            NOT NULL
        CONSTRAINT time_log_pk
            PRIMARY KEY AUTOINCREMENT,
    day_fk            INTEGER                            NOT NULL
        CONSTRAINT time_log_days_id_fk
            REFERENCES days,
    project_fk        INTEGER                            NOT NULL
        CONSTRAINT time_log_projects_id_fk
            REFERENCES projects,
    location_fk       INTEGER                            NOT NULL
        CONSTRAINT time_log_locations_id_fk
            REFERENCES locations,
    timestamp_created DATETIME DEFAULT CURRENT_TIMESTAMP NOT NULL,
    message           TEXT                               NOT NULL,
    mode              INTEGER                            NOT NULL
);

CREATE INDEX time_log_day_fk_timestamp_created_index
    ON time_log (day_fk, timestamp_created);

