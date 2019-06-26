-- Get Counters
select * from counters
where environment = @Environment
order by category, name;