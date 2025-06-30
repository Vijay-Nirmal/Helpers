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

    public async Task<DatabaseConnectionResponse> TestConnectionAsync(DatabaseConnectionRequest request)
    {
        try
        {
            using var connection = new SqlConnection(request.ConnectionString);
            await connection.OpenAsync();

            var command = new SqlCommand("SELECT @@VERSION, DB_NAME()", connection);
            using var reader = await command.ExecuteReaderAsync();
            
            if (await reader.ReadAsync())
            {
                var serverVersion = reader.GetString(0);
                var databaseName = reader.IsDBNull(1) ? "master" : reader.GetString(1);

                return new DatabaseConnectionResponse
                {
                    IsConnected = true,
                    Message = "Connection successful",
                    ServerVersion = serverVersion,
                    DatabaseName = databaseName
                };
            }

            return new DatabaseConnectionResponse
            {
                IsConnected = false,
                Message = "Unable to retrieve server information"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to test database connection");
            return new DatabaseConnectionResponse
            {
                IsConnected = false,
                Message = $"Connection failed: {ex.Message}"
            };
        }
    }

    public async Task<DeadlockEventsResponse> GetDeadlockEventsAsync(DeadlockEventsRequest request)
    {
        try
        {
            using var connection = new SqlConnection(request.ConnectionString);
            await connection.OpenAsync();

            // First, check if the session exists and is running
            var sessionCheckQuery = @"
                SELECT s.name, s.startup_state
                FROM sys.server_event_sessions s
                WHERE s.name = @SessionName";

            using var sessionCheckCommand = new SqlCommand(sessionCheckQuery, connection);
            sessionCheckCommand.Parameters.AddWithValue("@SessionName", request.ExtendedEventSessionName);
            
            using var sessionReader = await sessionCheckCommand.ExecuteReaderAsync();
            if (!await sessionReader.ReadAsync())
            {
                return new DeadlockEventsResponse
                {
                    Success = false,
                    Message = $"Extended Event session '{request.ExtendedEventSessionName}' not found"
                };
            }
            sessionReader.Close();

            // Build the query to get deadlock events
            var query = @"
                WITH DeadlockEvents AS (
                    SELECT 
                        object_name,
                        CAST(event_data AS XML) AS event_data_xml,
                        file_name,
                        file_offset,
                        timestamp_utc
                    FROM sys.fn_xe_file_target_read_file(
                        (SELECT CAST(t.target_data AS XML).value('(EventFileTarget/File/@name)[1]', 'NVARCHAR(256)')
                         FROM sys.dm_xe_sessions s
                         INNER JOIN sys.dm_xe_session_targets t ON s.address = t.event_session_address
                         WHERE s.name = @SessionName AND t.target_name = 'event_file'),
                        NULL, NULL, NULL
                    )
                    WHERE object_name = 'xml_deadlock_report'
                )
                SELECT TOP (@MaxRecords)
                    timestamp_utc,
                    event_data_xml.value('(event/data[@name=""xml_report""]/value)[1]', 'NVARCHAR(MAX)') AS xml_report,
                    @SessionName as session_name
                FROM DeadlockEvents
                WHERE (@StartDate IS NULL OR timestamp_utc >= @StartDate)
                  AND (@EndDate IS NULL OR timestamp_utc <= @EndDate)
                ORDER BY timestamp_utc DESC";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@SessionName", request.ExtendedEventSessionName);
            command.Parameters.AddWithValue("@MaxRecords", request.MaxRecords);
            command.Parameters.AddWithValue("@StartDate", (object?)request.StartDate ?? DBNull.Value);
            command.Parameters.AddWithValue("@EndDate", (object?)request.EndDate ?? DBNull.Value);
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

    public async Task<List<ExtendedEventSession>> GetExtendedEventSessionsAsync(string connectionString)
    {
        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            var query = @"
                SELECT 
                    s.name,
                    CASE WHEN rs.name IS NOT NULL THEN 'STARTED' ELSE 'STOPPED' END as state,
                    CASE WHEN rs.name IS NOT NULL THEN 1 ELSE 0 END as is_running,
                    s.create_time
                FROM sys.server_event_sessions s
                LEFT JOIN sys.dm_xe_sessions rs ON s.name = rs.name
                WHERE EXISTS (
                    SELECT 1 
                    FROM sys.server_event_session_events e 
                    WHERE e.event_session_id = s.event_session_id 
                    AND e.name = 'xml_deadlock_report'
                )
                ORDER BY s.name";

            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            var sessions = new List<ExtendedEventSession>();
            while (await reader.ReadAsync())
            {
                sessions.Add(new ExtendedEventSession
                {
                    Name = reader.GetString("name"),
                    State = reader.GetString("state"),
                    IsRunning = reader.GetBoolean("is_running"),
                    CreateTime = reader.IsDBNull("create_time") ? null : reader.GetDateTime("create_time")
                });
            }

            return sessions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve Extended Event sessions");
            return new List<ExtendedEventSession>();
        }
    }
}
