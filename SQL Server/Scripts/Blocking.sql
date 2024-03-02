--SPROC

SP_WHO2

--DMV

SELECT session_id, command, blocking_session_id, 
wait_type, wait_time, wait_resource, t.TEXT
FROM sys.dm_exec_requests 
CROSS apply sys.dm_exec_sql_text(sql_handle) AS t
WHERE session_id > 50 
AND blocking_session_id > 0
UNION
SELECT session_id, '', '', '', '', '', t.TEXT
FROM sys.dm_exec_connections 
CROSS apply sys.dm_exec_sql_text(most_recent_sql_handle) AS t
WHERE session_id IN (SELECT blocking_session_id 
FROM sys.dm_exec_requests 
WHERE blocking_session_id > 0)


SELECT * FROM sys.sysprocesses WHERE blocked > 0

DBCC INPUTBUFFER(51) -- Use this to see the last command (plain sql text) executed by a session

--ACTIVITY MONITOR

--RIGHT CLICK THE SERVER, CHOOSE ACTIVITY MONITOR

--SSMS REPORTS  (RIGHT CLICK DB - REPORTS - STANDARD REPORTS - ALL BLOCKING)

--SQL PROFILER (DEPRICATED)

--EXTENDED EVENTS