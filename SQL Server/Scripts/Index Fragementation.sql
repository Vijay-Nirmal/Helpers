--4 DETERMINE WHAT IS FRAGMENTED AFTER DATA INSERTS  (using the DMV sys.dm_db_index_physical_stats )

SELECT
DB_NAME(DATABASE_ID) AS [Database_Name],
OBJECT_NAME(OBJECT_ID) AS Table_Name,
SI.NAME AS Index_Name,
INDEX_TYPE_DESC AS Index_Type,
AVG_FRAGMENTATION_IN_PERCENT AS Avg_Page_Frag,
PAGE_COUNT AS Page_Counts
FROM sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL , NULL, N'LIMITED') DPS
INNER JOIN sysindexes SI
ON DPS.OBJECT_ID = SI.ID AND DPS.INDEX_ID = SI.INDID
GO

-- Setup a "Maintenance Plan" to run rebuild or reorganize index on a regular basis (maybe weekly in off business hours)

-- Reorganize Index when framgmentation is lesser than 30%
ALTER INDEX [<Index_Name>] ON <Table_Name>
REORGANIZE;
GO

-- Rebuild Index when framgmentation is lesser than 30%
ALTER INDEX [<Index_Name>] ON <Table_Name>
REBUILD WITH (ONLINE=ON);
GO

-- A 3rd path script to use is OLA => https://ola.hallengren.com/sql-server-index-and-statistics-maintenance.html