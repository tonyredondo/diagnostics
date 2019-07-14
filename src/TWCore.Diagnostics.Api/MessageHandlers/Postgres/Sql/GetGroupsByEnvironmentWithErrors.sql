-- Get Groups by environment
select *, (count(*) OVER()) as _query_totalcount from
(select environment, "group",
       max(logscount) as "logscount",
       max(tracescount) as "tracescount",
       min(start) as start,
       max("end") as "end",
       bool_or(haserror) as haserror
from
    (select environment, "group",
            0 as logscount,
            count(*) as tracescount,
           min(timestamp) as start,
           max(timestamp) as end,
           bool_or(position('Status: Error' in tags) > 0) as haserror
    from traces
    where environment = @Environment
      and timestamp between @FromDate and @ToDate
    group by environment, "group"
    union all
    select environment, "group",
           count(*) as logscount,
           0 as tracescount,
           min(timestamp) as start,
           max(timestamp) as end,
           bool_or(level = 1) as haserror
    from logs
    where environment = @Environment
      and date between @FromDate and @ToDate
    group by environment, "group") as raw

group by environment, "group") as grouped
where tracescount > 0 and haserror = true
order by start desc
limit @PageSize offset  @Page * @PageSize;