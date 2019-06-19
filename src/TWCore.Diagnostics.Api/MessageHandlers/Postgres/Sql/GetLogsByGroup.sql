-- Get Logs by Group and Time
select * from logs
where
      environment = @Environment
  and timestamp between @FromDate and @ToDate
  and "group" = @Group
order by timestamp;