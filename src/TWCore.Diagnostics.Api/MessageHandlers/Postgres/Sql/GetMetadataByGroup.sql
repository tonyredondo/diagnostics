-- Get metadata by group
select * from metadata
where (environment = @Environment or environment is null)
	and "group" = @Group
order by timestamp desc;