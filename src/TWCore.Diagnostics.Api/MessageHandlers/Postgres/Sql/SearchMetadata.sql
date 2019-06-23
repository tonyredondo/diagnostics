-- Search Metadata
select * from
    (select * from metadata
     where
        timestamp between @FromDate and @ToDate
     order by timestamp) as fmeta
where (value like @Search || '%')