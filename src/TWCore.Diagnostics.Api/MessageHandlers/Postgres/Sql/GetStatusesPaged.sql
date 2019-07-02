-- Get Status Paged
select *, (count(*) OVER()) as _query_totalcount from status
where environment = @Environment
    and (machine = @Machine or @Machine is null)
    and (application = @Application or @Application is null)
	and date between @FromDate and @ToDate
order by timestamp desc
limit @PageSize offset @Page * @PageSize;
