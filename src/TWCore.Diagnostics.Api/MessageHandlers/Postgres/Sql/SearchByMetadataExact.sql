-- Search by metadata exact
select distinct "group"
from metadata
where (environment = @Environment or environment is null)
	and date between @FromDate and @ToDate
    and key = @Key
    and (@Value = '*' or value = @Value)
limit @Limit;
