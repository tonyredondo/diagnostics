-- Search Logs
select * from
    (select * from logs
     where
           environment = @Environment
       and timestamp between @FromDate and @ToDate
     order by timestamp) as flogs
where ("group" like @Search || '%')