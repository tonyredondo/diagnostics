-- Get all log levels for an environment, timestamp
SELECT environment, application, CAST(timestamp as date), level
from logs
where environment = @Environment
  and timestamp between @FromDate and @ToDate
group by environment, application, CAST(timestamp as date), level
order by timestamp desc;