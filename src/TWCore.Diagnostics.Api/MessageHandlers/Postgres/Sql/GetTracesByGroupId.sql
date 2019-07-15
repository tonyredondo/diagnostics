-- Get Traces by group
select * from traces
where environment = @Environment
    and "group" = @Group;
--order by timestamp;