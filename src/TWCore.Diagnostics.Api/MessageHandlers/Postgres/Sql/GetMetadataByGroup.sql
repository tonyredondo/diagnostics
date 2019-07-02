-- Get metadata by group
select * from metadata
where "group" = @Group
order by timestamp desc;