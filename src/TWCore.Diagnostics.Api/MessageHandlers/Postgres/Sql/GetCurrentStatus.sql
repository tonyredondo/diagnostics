-- Get Status and Status Values
select s.*, sv.key, sv.value, sv.type from status s
inner join status_values sv on s.status_id = sv.status_id
where environment = @Environment
    and (machine = @Machine or @Machine is null)
    and (application = @Application or @Application is null)
order by timestamp desc