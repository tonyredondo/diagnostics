-- Search by metadata
select distinct "group"
from metadata
where date between @FromDate and @ToDate
    and key = @Key
    and  (value like @Value || '%')
limit @Limit;