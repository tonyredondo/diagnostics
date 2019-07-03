-- Search Group
select distinct "group"
from logs where environment = @Environment and date between @FromDate and @ToDate
and "group" = @Search
union distinct
select distinct "group"
from metadata where date between @FromDate and @ToDate
and value = @Search
union distinct
select distinct "group"
from traces where environment = @Environment and timestamp between @FromDate and @ToDate
and "group" = @Search
limit @Limit;
