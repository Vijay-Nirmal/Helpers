using DeadlockAnalyzer.Models;
using DeadlockAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;

namespace DeadlockAnalyzer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DeadlockController : ControllerBase
{
    private readonly IDeadlockService _deadlockService;
    private readonly ILogger<DeadlockController> _logger;

    public DeadlockController(IDeadlockService deadlockService, ILogger<DeadlockController> logger)
    {
        _deadlockService = deadlockService;
        _logger = logger;
    }

    [HttpPost("test-connection")]
    public async Task<ActionResult<DatabaseConnectionResponse>> TestConnection([FromBody] DatabaseConnectionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            return BadRequest(new DatabaseConnectionResponse
            {
                IsConnected = false,
                Message = "Connection string is required"
            });
        }

        try
        {
            var result = await _deadlockService.TestConnectionAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error testing database connection");
            return StatusCode(500, new DatabaseConnectionResponse
            {
                IsConnected = false,
                Message = "Internal server error occurred while testing connection"
            });
        }
    }

    [HttpGet("extended-event-sessions")]
    public async Task<ActionResult<List<ExtendedEventSession>>> GetExtendedEventSessions([FromQuery] string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return BadRequest("Connection string is required");
        }

        try
        {
            var sessions = await _deadlockService.GetExtendedEventSessionsAsync(connectionString);
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Extended Event sessions");
            return StatusCode(500, "Internal server error occurred while retrieving Extended Event sessions");
        }
    }

    [HttpPost("deadlock-events")]
    public async Task<ActionResult<DeadlockEventsResponse>> GetDeadlockEvents([FromBody] DeadlockEventsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            return BadRequest(new DeadlockEventsResponse
            {
                Success = false,
                Message = "Connection string is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.ExtendedEventSessionName))
        {
            return BadRequest(new DeadlockEventsResponse
            {
                Success = false,
                Message = "Extended Event session name is required"
            });
        }

        try
        {
            var result = await _deadlockService.GetDeadlockEventsAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving deadlock events");
            return StatusCode(500, new DeadlockEventsResponse
            {
                Success = false,
                Message = "Internal server error occurred while retrieving deadlock events"
            });
        }
    }

    [HttpPost("execute-diagnostic-query")]
    public async Task<ActionResult<DiagnosticQueryResponse>> ExecuteDiagnosticQuery([FromBody] DiagnosticQueryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            return BadRequest(new DiagnosticQueryResponse
            {
                Success = false,
                Message = "Connection string is required"
            });
        }

        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest(new DiagnosticQueryResponse
            {
                Success = false,
                Message = "Query is required"
            });
        }

        // Security check - prevent potentially dangerous operations
        var upperQuery = request.Query.ToUpper();
        var dangerousKeywords = new[] { "DROP", "DELETE", "TRUNCATE", "INSERT", "UPDATE", "ALTER", "CREATE", "EXEC", "EXECUTE" };
        
        if (dangerousKeywords.Any(keyword => upperQuery.Contains(keyword)))
        {
            return BadRequest(new DiagnosticQueryResponse
            {
                Success = false,
                Message = "Only SELECT queries are allowed for diagnostic purposes"
            });
        }

        try
        {
            var result = await _deadlockService.ExecuteDiagnosticQueryAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing diagnostic query");
            return StatusCode(500, new DiagnosticQueryResponse
            {
                Success = false,
                Message = "Internal server error occurred while executing diagnostic query"
            });
        }
    }

    [HttpGet("predefined-queries")]
    public ActionResult<List<PredefinedQuery>> GetPredefinedQueries()
    {
        var queries = new List<PredefinedQuery>
        {
            new PredefinedQuery
            {
                Name = "Active Deadlock Sessions",
                Description = "Shows Extended Event sessions that capture deadlock events",
                Query = @"
SELECT 
    s.name AS session_name,
    CASE WHEN rs.name IS NOT NULL THEN 'STARTED' ELSE 'STOPPED' END AS state,
    s.create_time,
    COUNT(e.name) AS deadlock_events_count
FROM sys.server_event_sessions s
LEFT JOIN sys.dm_xe_sessions rs ON s.name = rs.name
LEFT JOIN sys.server_event_session_events e ON s.event_session_id = e.event_session_id AND e.name = 'xml_deadlock_report'
WHERE EXISTS (
    SELECT 1 FROM sys.server_event_session_events se 
    WHERE se.event_session_id = s.event_session_id AND se.name = 'xml_deadlock_report'
)
GROUP BY s.name, rs.name, s.create_time
ORDER BY s.name"
            },
            new PredefinedQuery
            {
                Name = "Lock Waits Summary",
                Description = "Shows current lock wait statistics",
                Query = @"
SELECT 
    wait_type,
    waiting_tasks_count,
    wait_time_ms,
    max_wait_time_ms,
    signal_wait_time_ms,
    wait_time_ms - signal_wait_time_ms AS resource_wait_time_ms,
    CASE 
        WHEN waiting_tasks_count > 0 
        THEN wait_time_ms / waiting_tasks_count 
        ELSE 0 
    END AS avg_wait_time_ms
FROM sys.dm_os_wait_stats
WHERE wait_type LIKE 'LCK%'
   OR wait_type IN ('DEADLOCK_MONITOR', 'LOCK_MONITOR')
ORDER BY wait_time_ms DESC"
            },
            new PredefinedQuery
            {
                Name = "Current Blocking Processes",
                Description = "Shows currently blocking and blocked processes",
                Query = @"
SELECT 
    r.session_id AS blocked_session_id,
    r.blocking_session_id,
    r.wait_type,
    r.wait_time,
    r.wait_resource,
    r.command,
    s.program_name,
    s.host_name,
    s.login_name,
    r.status,
    t.text AS current_statement
FROM sys.dm_exec_requests r
INNER JOIN sys.dm_exec_sessions s ON r.session_id = s.session_id
OUTER APPLY sys.dm_exec_sql_text(r.sql_handle) t
WHERE r.blocking_session_id > 0
   OR r.session_id IN (SELECT blocking_session_id FROM sys.dm_exec_requests WHERE blocking_session_id > 0)
ORDER BY r.blocking_session_id, r.session_id"
            },
            new PredefinedQuery
            {
                Name = "Database Lock Usage",
                Description = "Shows lock counts by database and resource type",
                Query = @"
SELECT 
    DB_NAME(resource_database_id) AS database_name,
    resource_type,
    request_mode,
    COUNT(*) AS lock_count
FROM sys.dm_tran_locks
WHERE resource_database_id > 0
GROUP BY resource_database_id, resource_type, request_mode
ORDER BY DB_NAME(resource_database_id), resource_type, lock_count DESC"
            },
            new PredefinedQuery
            {
                Name = "Top Queries by CPU",
                Description = "Shows queries with highest CPU usage that might be involved in deadlocks",
                Query = @"
SELECT TOP 20
    qs.sql_handle,
    qs.execution_count,
    qs.total_worker_time,
    qs.avg_worker_time,
    qs.total_elapsed_time,
    qs.avg_elapsed_time,
    qs.total_logical_reads,
    qs.avg_logical_reads,
    t.text AS query_text
FROM sys.dm_exec_query_stats qs
CROSS APPLY sys.dm_exec_sql_text(qs.sql_handle) t
ORDER BY qs.avg_worker_time DESC"
            }
        };

        return Ok(queries);
    }
}
