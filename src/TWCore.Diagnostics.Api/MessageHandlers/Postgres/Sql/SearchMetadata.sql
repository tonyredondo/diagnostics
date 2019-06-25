-- Search Metadata
select * from
    (select * from metadata
     where
        date between @FromDate and @ToDate
     order by date) as fmeta
where (value like @Search || '%')