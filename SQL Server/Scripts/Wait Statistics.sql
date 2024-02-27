-- List of queries to get wait statistics

/*
The sys.dm_os_wait_stats view returns information about the various wait types encountered by SQL Server since the last restart.
This information can be used to identify and troubleshoot performance issues related to waits and resource contention.
*/
Select * from sys.dm_os_wait_stats

/*
This query retrieves information about the waiting tasks in the SQL Server instance.
It uses the sys.dm_os_waiting_tasks dynamic management view to get details such as the type of wait, the resource being waited on, and the session ID of the task.
*/
Select * from sys.dm_os_waiting_tasks

/*
The sys.dm_exec_requests view returns information about each request that is executing within SQL Server.
This query can be used to monitor the current requests and their associated wait statistics.
*/
Select * from sys.dm_exec_requests