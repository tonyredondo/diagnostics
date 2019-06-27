-- Get Status and Status Values
select s.*, sv.key, sv.value, sv.type, (count(*) OVER()) as _query_totalcount 
from status s
inner join status_values sv on s.status_id = sv.status_id
where environment = @Environment
    and (machine = @Machine or @Machine is null)
    and (application = @Application or @Application is null)
	and date between @FromDate and @ToDate
order by timestamp desc
limit @PageSize offset @Page * @PageSize;