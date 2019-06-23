-- Get Traces by environment
select environment, "group", count(*),
       min(timestamp) as start,
       max(timestamp) as end,
       bool_or(position('Status: Error' in tags) > 0) as haserror
from traces
where environment = @Environment
  and timestamp between @FromDate and @ToDate
group by environment, "group"
order by start
limit @PageSize offset @Page * @PageSize;