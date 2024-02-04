-- An auto-growth event is the process by which the SQL Server engine expands the size of a database file when it runs out of space. Each database has its own auto-growth setting – that is set by the default setting of the model database. Rarely do we use the model setting.
-- If you don’t set up the auto-growth setting for each database and be proactive in your duties, then each time an auto-growth event is performed, SQL Server holds up database processing while an auto-growth event occurs. And this can cause poor performance.

Select
s.[name] AS [Logical Name]
,S.[file_id] AS [File ID]
, S.[physical_name] AS [File Name]
,Cast(CAST(G.name AS VARBINARY(256)) AS sysname) AS [FileGroup_Name]
,CONVERT (varchar(10),(S.[size]*8)) + 'KB' AS [Size]
,CASE WHEN S.[max_size] = -1 THEN 'Unlimited' ELSE CONVERT(VARCHAR(10), CONVERT(bigint,S.[max_size])*8) +'KB' END AS [Max Size]
,CASE s.is_percent_growth WHEN 1 THEN CONVERT(VARCHAR(10), S.growth) +'%' ELSE Convert(VARCHAR(10), S.growth*8) +' KB' END AS [Growth]
, Case WHEN S.[type]=0 THEN 'Data Only'
WHEN S.[type]=1 THEN 'log Only'
WHEN S.[type]=2 THEN 'FILESTREAM Only'
WHEN S.[type]=3 THEN 'Informational purpose Only'
WHEN S.[type]=4 THEN 'Full-text '
END AS [usage]
,DB_name(S.database_id) AS [Database Name]
From sys.master_files AS S
LEFT JOIN sys.filegroups AS G ON ((S.type = 2 OR S.type = 0)
AND (S.drop_lsn IS NULL)) AND (S.data_space_id=G.data_space_id)