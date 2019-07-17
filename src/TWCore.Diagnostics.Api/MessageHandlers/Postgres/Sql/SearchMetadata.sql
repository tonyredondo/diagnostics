-- Search Metadata
select * from
    (select * from metadata
     where
		(environment = @Environment or environment is null)
        and date between @FromDate and @ToDate
     order by date) as fmeta
where (value like @Search || '%');