
create table logs
(
    log_id      uuid not null
        constraint logs_pk
            primary key,
    environment varchar(128),
    machine     varchar(128),
    application varchar(512),
    timestamp   timestamp,
    assembly    varchar(1024),
    type        varchar(1024),
    "group"     varchar(512),
    code        varchar(128),
    level       integer,
    message     varchar,
    exception   json,
    date        date
);

create unique index logs_logid_uindex
    on logs (log_id);

create index logs_env
    on logs (environment);

create index logs_env_date
    on logs (environment asc, date desc);

create index logs_byapp
    on logs (environment asc, application asc, date desc, level asc);

create index logs_search
    on logs (environment, date, "group");

create index logs_bygroup
    on logs (environment, "group");

create table traces
(
    trace_id    uuid not null
        constraint traces_pk
            primary key,
    environment varchar(128),
    machine     varchar(128),
    application varchar(512),
    timestamp   timestamp,
    tags        varchar(2048),
    "group"     varchar(512),
    name        varchar(1024),
    formats     text[]
);

create index traces_search
    on traces (environment asc, timestamp desc, application asc, "group" asc);

create index traces_bygroup
    on traces (environment, "group");

create table counters
(
    counter_id  uuid not null
        constraint counters_pk
            primary key,
    environment varchar(128),
    application varchar(512),
    category    varchar(512),
    name        varchar(1024),
    type        integer,
    level       integer,
    kind        integer,
    unit        integer,
    typeofvalue varchar(512)
);

create index counters_search
    on counters (environment, application, category);

create index counters_env
    on counters (environment);

create index counters_new
    on counters (environment, application, category, name);

create table counters_values
(
    counter_id uuid      not null
        constraint counters_values_counters_counterid_fk
            references counters
            on update cascade on delete cascade,
    timestamp  timestamp not null,
    value      real,
    constraint counters_values_pk
        primary key (counter_id, timestamp)
);

create table metadata
(
    "group"     varchar(512) not null,
    environment varchar(128),
    timestamp   timestamp,
    key         varchar(512),
    value       varchar(512),
    date        date
);

create index metadata_bygroup
    on metadata (environment, "group");

create index metadata_byvalue
    on metadata (environment, timestamp, value);

create index metadata_search2
    on metadata (environment, date, key, value);

create index metadata_search
    on metadata (environment, date, value);

create table status
(
    status_id           uuid not null
        constraint status_pk
            primary key,
    environment         varchar(128),
    machine             varchar(128),
    application         varchar(512),
    timestamp           timestamp,
    application_display varchar(512),
    elapsed             numeric,
    start_time          timestamp,
    date                date
);

create unique index status_statusid_uindex
    on status (status_id);

create table status_values
(
    status_id uuid          not null
        constraint status_values_status_statusid_fk
            references status
            on update cascade on delete cascade,
    key       varchar(2048) not null,
    value     varchar(2048),
    type      integer,
    constraint status_values_pk
        primary key (status_id, key)
);

