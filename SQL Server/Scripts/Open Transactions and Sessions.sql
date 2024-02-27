
/*
This query executes the stored procedure sp_who2, which provides information about the current sessions and open transactions in the SQL Server instance.
It can be used to monitor and troubleshoot database activity, identify blocking processes, and view information about active connections.
*/
EXEC sp_who2

/*
This query retrieves information about open transactions and sessions in SQL Server.
Use this query is to help identify and monitor active transactions and sessions in the database.
It can be useful for troubleshooting performance issues, identifying blocking scenarios, and monitoring
the overall health of the database.
*/
SELECT spid,blocked,loginame,cmd,text,lastwaittype,physical_io,login_time,
open_tran,status,hostname
FROM SYS.SYSPROCESSES SP
CROSS APPLY SYS.DM_EXEC_SQL_TEXT(SP.[SQL_HANDLE])
AS DEST WHERE OPEN_TRAN >= 1