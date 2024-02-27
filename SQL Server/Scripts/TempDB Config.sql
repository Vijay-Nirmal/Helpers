/*
 One of the poor performance objects may the tempdb. The tempdb system database is a global resource that is available to all users
 - Global or local temporary tables
 - Temporary stored procedures 
 - Table variables
 - Cursors
 - Results for spools or sorting
 - Online index operations
 - Multiple Active Result Sets (MARS)
 - AFTER triggers. 
 - Tempdb is re-created every time SQL Server is started 
 - Backup and restore operations are not allowed on tempdb*
 
 Out of the box we may get into issues of poor performance because of the default settings.  
 The following list is something we must do asap
 - Default file numbers change for tempdb
 - Default size change for tempdb
 - Set the file growth increment
 */
-- File number rule: Tempdb files based on number of CPU cores
-- When you have less (or equal) than 8 CPU cores, you will get as many TempDb data files as you have CPU cores.
-- If you have more than 8 CPU cores, you will get 8 TempDb data files out of the box.
--COUNT CPU
SELECT
    cpu_count,
    *
FROM
    sys.dm_os_sys_info

--DETAILED LOOK INTO CPU COUNT
DECLARE @xp_msver TABLE (
    [idx] [int] NULL,
    [c_name] [varchar](100) NULL,
    [int_val] [float] NULL,
    [c_val] [varchar](128) NULL
)
INSERT INTO
    @xp_msver EXEC ('[master]..[xp_msver]');

;

WITH [ProcessorInfo] AS (
    SELECT
        ([cpu_count] / [hyperthread_ratio]) AS [number_of_physical_cpus],
CASE
            WHEN hyperthread_ratio = cpu_count THEN cpu_count
            ELSE (
                ([cpu_count] - [hyperthread_ratio]) / ([cpu_count] / [hyperthread_ratio])
            )
        END AS [number_of_cores_per_cpu],
CASE
            WHEN hyperthread_ratio = cpu_count THEN cpu_count
            ELSE ([cpu_count] / [hyperthread_ratio]) * (
                ([cpu_count] - [hyperthread_ratio]) / ([cpu_count] / [hyperthread_ratio])
            )
        END AS [total_number_of_cores],
        [cpu_count] AS [number_of_virtual_cpus],
(
            SELECT
                [c_val]
            FROM
                @xp_msver
            WHERE
                [c_name] = 'Platform'
        ) AS [cpu_category]
    FROM
        [sys].[dm_os_sys_info]
)
SELECT
    [number_of_physical_cpus],
    [number_of_cores_per_cpu],
    [total_number_of_cores],
    [number_of_virtual_cpus],
    LTRIM(
        RIGHT(
            [cpu_category],
            CHARINDEX('x', [cpu_category]) - 1
        )
    ) AS [cpu_category]
FROM
    [ProcessorInfo]


-- Changing the size of tempdb
ALTER DATABASE [tempdb]
MODIFY
    FILE (
        NAME = N'tempdev',
        SIZE = 512000KB,
        FILEGROWTH = 102400KB
    )
GO
ALTER DATABASE [tempdb]
MODIFY
    FILE (
        NAME = N'templog',
        SIZE = 51200KB,
        FILEGROWTH = 102400KB
    )
    
    
-- Set the file growth increment
-- TempDB file size    FILEGROWTH increment
-- 0 MB to 100 MB      10 MB
-- 100 MB to 200 MB    20 MB
-- 200 MB or more      10%*
-- view the size and file growth of the tempdb
SELECT
    name AS FileName,
    size * 1.0 / 128 AS FileSizeinMB,
    CASE
        max_size
        WHEN 0 THEN 'Autogrowth is turned off.'
        WHEN -1 THEN 'Autogrowth is turned on.'
        ELSE 'Log file will continue to grow'
    END,
    growth AS 'GrowthValue',
    'GrowthIncrement' = CASE
        WHEN growth = 0 THEN 'Size is fixed and will not grow.'
        WHEN growth > 0
        AND is_percent_growth = 0 THEN 'Growth value is in 8-KB pages.'
        ELSE 'Growth value is a percentage.'
    END
FROM
    tempdb.sys.database_files;

GO