-- Get Logs by Group
select * from logs
where
      environment = @Environment
  and "group" = @Group;
--order by timestamp;