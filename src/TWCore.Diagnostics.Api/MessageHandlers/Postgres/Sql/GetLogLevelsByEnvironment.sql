-- Get all log levels for an environment, timestamp
SELECT environment, application, date, level, count(level)
from logs
where environment = @Environment
  and date between @FromDate and @ToDate
group by environment, application, date, level
--order by date desc;