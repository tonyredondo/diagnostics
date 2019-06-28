-- Get Counter
select * from counters
where environment = @Environment
	and application = @Application
	and category = @Category
	and name = @Name
limit 1