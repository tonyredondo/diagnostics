-- Search Group
select distinct "group"
from logs where environment = @Environment and date between @FromDate and @ToDate
and ("group" ilike @Search || '%')
union distinct
select distinct "group"
from metadata where (environment = @Environment or environment is null)
	and date between @FromDate and @ToDate
and (value ilike @Search || '%')
union distinct
select distinct "group"
from traces where environment = @Environment and timestamp between @FromDate and @ToDate
and ("group" ilike @Search || '%' or application ilike @Search || '%')
limit @Limit;
