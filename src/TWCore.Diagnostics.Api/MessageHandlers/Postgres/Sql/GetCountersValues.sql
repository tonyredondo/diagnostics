-- Get Counters Values
select * from counters_values
where counter_id = @CounterId
    and timestamp between @FromDate and @ToDate
order by timestamp desc;