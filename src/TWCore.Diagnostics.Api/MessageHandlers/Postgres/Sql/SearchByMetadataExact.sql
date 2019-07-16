-- Search by metadata exact
select distinct "group"
from metadata
where date between @FromDate and @ToDate
    and key = @Key
    and value = @Value
limit @Limit;
