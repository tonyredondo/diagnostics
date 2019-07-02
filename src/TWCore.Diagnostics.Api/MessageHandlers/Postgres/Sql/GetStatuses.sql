-- Get Statuses
select * from status
where environment = @Environment
    and (machine = @Machine or @Machine is null)
    and (application = @Application or @Application is null)
order by timestamp desc