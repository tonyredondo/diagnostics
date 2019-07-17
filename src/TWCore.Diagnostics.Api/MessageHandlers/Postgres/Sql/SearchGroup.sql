-- Search Group
select distinct "group"
from logs where environment = @Environment and date between @FromDate and @ToDate
and ("group" like @Search || '%')
union distinct
select distinct "group"
from metadata where (environment = @Environment or environment is null)
	and date between @FromDate and @ToDate
and (value like @Search || '%')
union distinct
select distinct "group"
from traces where environment = @Environment and timestamp between @FromDate and @ToDate
and ("group" like @Search || '%')
limit @Limit;
