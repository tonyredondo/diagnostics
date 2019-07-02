-- Get Counter by id
select * from counters
where counter_id = @CounterId
order by category, name;