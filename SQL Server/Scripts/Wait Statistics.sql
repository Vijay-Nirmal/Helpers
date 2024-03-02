-- List of queries to get wait statistics

/*
The sys.dm_os_wait_stats view returns information about the various wait types encountered by SQL Server since the last restart.
This information can be used to identify and troubleshoot performance issues related to waits and resource contention.
*/

--waiting_tasks_count:     Number of waits on this wait type.  
--wait_time_ms:            Total wait time for this wait type in milliseconds. (1000th of second)
--max_wait_time_ms:        Maximum wait time on this wait type.

Select * from sys.dm_os_wait_stats

--While there are many wait stat types, you should focus more of the common one such as the following:
--CXPACKET, PAGEIOLATCH_XX, LCK_M_X, ASYNC_NETWORK_IO
-- Details about Wait type: https://docs.microsoft.com/en-us/sql/relational-databases/system-dynamic-management-views/sys-dm-os-wait-stats-transact-sql
SELECT *
FROM sys.dm_os_wait_stats 
WHERE wait_type = 'LCK_M_S'  --LCK_M_X

-- Reset wait statistics
DBCC SQLPERF (N'sys.dm_os_wait_stats', CLEAR);

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



--CREATE AN EXTENDED EVENT SESSION TO CAPTURE DATA FOR WAIT STATS

CREATE EVENT SESSION [CAPTURE WAIT STATS ON LOCK] 

ON SERVER 

ADD EVENT sqlos.wait_info

(

ACTION

(sqlserver.database_name,
sqlserver.nt_username,
sqlserver.session_id,
sqlserver.sql_text,sqlserver.transaction_id)

)
ADD TARGET package0.event_file

(SET filename=N'CAPTURE WAIT STATS ON LOCK')