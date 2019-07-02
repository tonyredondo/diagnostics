-- Get all log for an environment, application, timestamp
SELECT *, (count(*) OVER()) as _query_totalcount
from logs
where environment = @Environment
  and application = @Application
  and date between @FromDate and @ToDate
order by timestamp desc
limit @PageSize offset @Page * @PageSize;