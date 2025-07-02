using DeadlockAnalyzer.Models;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.Text.Json;

namespace DeadlockAnalyzer.Services;

public class DeadlockService : IDeadlockService
{
    private readonly ILogger<DeadlockService> _logger;

    public DeadlockService(ILogger<DeadlockService> logger)
    {
        _logger = logger;
    }

    public async Task<DeadlockEventsResponse> GetDeadlockEventsAsync(DeadlockEventsRequest request)
    {
        try
        {
            using var connection = new SqlConnection(request.ConnectionString);
            await connection.OpenAsync();

            // Query to get deadlock information from Extended Events
            var sessionName = string.IsNullOrWhiteSpace(request.ExtendedEventSessionName) ? "system_health" : request.ExtendedEventSessionName;
            var maxRecords = request.MaxRecords > 0 ? request.MaxRecords : 100;
            
            var whereClause = "WHERE object_name = 'xml_deadlock_report'";
            if (request.StartDate.HasValue || request.EndDate.HasValue)
            {
                if (request.StartDate.HasValue && request.EndDate.HasValue)
                {
                    whereClause += $" AND timestamp_utc BETWEEN '{request.StartDate.Value:yyyy-MM-dd HH:mm:ss}' AND '{request.EndDate.Value:yyyy-MM-dd HH:mm:ss}'";
                }
                else if (request.StartDate.HasValue)
                {
                    whereClause += $" AND timestamp_utc >= '{request.StartDate.Value:yyyy-MM-dd HH:mm:ss}'";
                }
                else if (request.EndDate.HasValue)
                {
                    whereClause += $" AND timestamp_utc <= '{request.EndDate.Value:yyyy-MM-dd HH:mm:ss}'";
                }
            }
            
            var query = $@"
                WITH DeadlockEvents AS (
                    SELECT 
                        object_name,
                        CAST(event_data AS XML) AS event_data_xml,
                        timestamp_utc
                    FROM sys.fn_xe_file_target_read_file(
                        (SELECT LEFT(CAST(t.target_data AS XML).value('(EventFileTarget/File/@name)[1]', 'NVARCHAR(256)'), 
                            LEN(CAST(t.target_data AS XML).value('(EventFileTarget/File/@name)[1]', 'NVARCHAR(256)')) -
                            CHARINDEX('_', REVERSE(CAST(t.target_data AS XML).value('(EventFileTarget/File/@name)[1]', 'NVARCHAR(256)')))) + '*.xel'
                         FROM sys.dm_xe_sessions s
                         INNER JOIN sys.dm_xe_session_targets t ON s.address = t.event_session_address
                         WHERE s.name = '{sessionName}' AND t.target_name = 'event_file'),
                        NULL, NULL, NULL
                    )
                    {whereClause}
                )
                SELECT TOP ({maxRecords})
                    timestamp_utc,
                    CAST(event_data_xml.query('(event/data[@name=""xml_report""]/value)[1]') AS NVARCHAR(MAX)) AS xml_report,
                    '{sessionName}' as session_name
                FROM DeadlockEvents
                ORDER BY timestamp_utc DESC";

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 60;

            var events = new List<DeadlockEvent>();
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var deadlockEvent = new DeadlockEvent
                {
                    Timestamp = reader.GetDateTime("timestamp_utc"),
                    XmlReport = reader.IsDBNull("xml_report") ? string.Empty : reader.GetString("xml_report"),
                    SessionName = reader.GetString("session_name")
                };
                events.Add(deadlockEvent);
            }

            return new DeadlockEventsResponse
            {
                Success = true,
                Message = $"Retrieved {events.Count} deadlock events",
                Events = events,
                TotalRecords = events.Count
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve deadlock events from Extended Events");
            return new DeadlockEventsResponse
            {
                Success = false,
                Message = $"Failed to retrieve deadlock events: {ex.Message}"
            };
        }
    }

    public async Task<DiagnosticQueryResponse> ExecuteDiagnosticQueryAsync(DiagnosticQueryRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            using var connection = new SqlConnection(request.ConnectionString);
            await connection.OpenAsync();

            using var command = new SqlCommand(request.Query, connection);
            command.CommandTimeout = request.TimeoutSeconds;
            
            using var reader = await command.ExecuteReaderAsync();
            
            var results = new List<Dictionary<string, object?>>();
            var columnNames = new List<string>();
            
            // Get column names
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }
            
            // Read data
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    
                    // Convert some special types to strings for JSON serialization
                    if (value != null)
                    {
                        if (value is DateTime dt)
                            value = dt.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        else if (value is TimeSpan ts)
                            value = ts.ToString();
                        else if (value is decimal || value is float || value is double)
                            value = value.ToString();
                    }
                    
                    row[columnNames[i]] = value;
                }
                results.Add(row);
            }

            stopwatch.Stop();

            return new DiagnosticQueryResponse
            {
                Success = true,
                Message = "Query executed successfully",
                Results = results,
                ColumnNames = columnNames,
                RecordCount = results.Count,
                ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Failed to execute diagnostic query");
            
            return new DiagnosticQueryResponse
            {
                Success = false,
                Message = $"Query execution failed: {ex.Message}",
                ExecutionTimeMs = stopwatch.Elapsed.TotalMilliseconds
            };
        }
    }

    public List<PredefinedQuery> GetPredefinedQueries()
    {
        return new List<PredefinedQuery>
        {
            new PredefinedQuery
            {
                Name = "Current Active Sessions",
                Description = "Shows currently active database sessions",
                Query = @"
                    SELECT 
                        s.session_id,
                        s.login_name,
                        s.host_name,
                        s.program_name,
                        s.status,
                        s.cpu_time,
                        s.memory_usage,
                        s.total_scheduled_time,
                        s.total_elapsed_time,
                        s.last_request_start_time,
                        s.last_request_end_time,
                        r.blocking_session_id,
                        r.wait_type,
                        r.wait_time,
                        r.wait_resource
                    FROM sys.dm_exec_sessions s
                    LEFT JOIN sys.dm_exec_requests r ON s.session_id = r.session_id
                    WHERE s.is_user_process = 1
                    ORDER BY s.cpu_time DESC"
            },
            new PredefinedQuery
            {
                Name = "Blocking Sessions",
                Description = "Shows sessions that are blocking other sessions",
                Query = @"
                    SELECT 
                        blocking.session_id AS blocking_session_id,
                        blocking.login_name AS blocking_login,
                        blocking.host_name AS blocking_host,
                        blocking.program_name AS blocking_program,
                        blocked.session_id AS blocked_session_id,
                        blocked.login_name AS blocked_login,
                        blocked.wait_type,
                        blocked.wait_time,
                        blocked.wait_resource,
                        st.text AS blocking_sql_text
                    FROM sys.dm_exec_requests blocked
                    INNER JOIN sys.dm_exec_sessions blocking ON blocked.blocking_session_id = blocking.session_id
                    OUTER APPLY sys.dm_exec_sql_text(blocking.most_recent_sql_handle) st
                    WHERE blocked.blocking_session_id > 0
                    ORDER BY blocked.wait_time DESC"
            },
            new PredefinedQuery
            {
                Name = "Lock Statistics",
                Description = "Shows current lock statistics by resource type",
                Query = @"
                    SELECT 
                        resource_type,
                        resource_database_id,
                        DB_NAME(resource_database_id) AS database_name,
                        request_mode,
                        request_status,
                        COUNT(*) AS lock_count
                    FROM sys.dm_tran_locks
                    WHERE resource_database_id > 0
                    GROUP BY resource_type, resource_database_id, request_mode, request_status
                    ORDER BY lock_count DESC"
            },
            new PredefinedQuery
            {
                Name = "Recent Deadlocks (Error Log)",
                Description = "Shows recent deadlock information from SQL Server error log",
                Query = @"
                    EXEC xp_readerrorlog 0, 1, N'deadlock';"
            }
        };
    }
}
