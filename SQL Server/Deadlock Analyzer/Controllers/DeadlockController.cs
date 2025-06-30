using DeadlockAnalyzer.Models;
using DeadlockAnalyzer.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

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

    [HttpPost("extended-events")]
    public async Task<ActionResult<string>> GetExtendedEvents([FromBody] ExtendedEventsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ConnectionString))
        {
            return BadRequest("Connection string is required");
        }

        try
        {
            var result = await _deadlockService.GetDeadlockEventsAsync(request);
            
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            // Combine all XML reports into a single response
            var combinedXml = string.Join("\n", result.Events.Select(e => e.XmlReport));
            
            return Ok(combinedXml);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving extended events");
            return StatusCode(500, "Internal server error occurred while retrieving extended events");
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
        var queries = _deadlockService.GetPredefinedQueries();
        return Ok(queries);
    }
}
