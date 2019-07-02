-- Get Logs by Group and Time
select * from logs
where
      environment = @Environment
  and date between @FromDate and @ToDate
  and "group" = @Group
order by timestamp;