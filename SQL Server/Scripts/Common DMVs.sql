-- https://technet.microsoft.com/en-us/library/ms188754(v=sql.110).aspx

--check open connections

SELECT *
FROM sys.dm_exec_connections


--check open sessions

SELECT 
session_id, 
login_time,
host_name,
program_name,
login_name,
nt_user_name
FROM sys.dm_exec_sessions
order by 5 desc


--combine DMVs  (find session id of login)


SELECT c.session_id
, c.auth_scheme
, c.node_affinity
, s.login_name
, db_name(s.database_id) AS database_name
, s.status
, c.most_recent_sql_handle
FROM sys.dm_exec_connections c
INNER JOIN sys.dm_exec_sessions s
ON c.session_id = s.session_id


--find average CPU time by query

SELECT TOP 10 total_worker_time/execution_count AS [Avg CPU Time],  
SUBSTRING(st.text, (qs.statement_start_offset/2)+1,   
((CASE qs.statement_end_offset  
WHEN -1 THEN DATALENGTH(st.text)  
ELSE qs.statement_end_offset  
END - qs.statement_start_offset)/2) + 1) AS statement_text  
FROM sys.dm_exec_query_stats AS qs  
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) AS st  
ORDER BY total_worker_time/execution_count DESC;  
 


--common DMVs to know

-- sys.dm_exec_connections: Returns information about the current connections to the SQL Server instance.
select * from  sys.dm_exec_connections

-- sys.dm_exec_sessions: Returns information about the active sessions on the SQL Server instance.
select * from  sys.dm_exec_sessions

-- sys.dm_exec_requests: Returns information about the currently executing requests on the SQL Server instance.
select * from  sys.dm_exec_requests

-- sys.dm_exec_cached_plans: Returns information about the cached query plans in the SQL Server instance.
select * from  sys.dm_exec_cached_plans

-- sys.dm_exec_query_stats: Returns information about the query execution statistics in the SQL Server instance.
select * from  sys.dm_exec_query_stats

-- sys.dm_db_index_usage_stats: Returns information about the usage of indexes in the SQL Server instance.
select * from  sys.dm_db_index_usage_stats

--SQL Server Operating System

-- This query retrieves performance counters for the SQL Server instance.
select * from sys.dm_os_performance_counters

-- This query retrieves information about tasks that are waiting for resources in the SQL Server instance.
select * from sys.dm_os_waiting_tasks

-- This query retrieves wait statistics for the SQL Server instance.
select * from sys.dm_os_wait_stats