-- Search Logs
select * from
    (select * from logs
     where
           environment = @Environment
       and timestamp between @FromDate and @ToDate
     order by timestamp limit 2000000) as flogs
where ("group" = @Search or message like concat('% ', @Search, ' %'))