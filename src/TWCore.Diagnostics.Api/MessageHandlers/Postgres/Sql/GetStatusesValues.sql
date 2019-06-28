-- Get Statuses Values
select * from status_values
where status_id = Any(@Ids)
order by status_id