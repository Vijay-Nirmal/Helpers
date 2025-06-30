namespace DeadlockAnalyzer.Models;

public class DeadlockEventsRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ExtendedEventSessionName { get; set; } = string.Empty;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int MaxRecords { get; set; } = 1000;
}

public class DeadlockEventsResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<DeadlockEvent> Events { get; set; } = new();
    public int TotalRecords { get; set; }
}

public class DeadlockEvent
{
    public DateTime Timestamp { get; set; }
    public string XmlReport { get; set; } = string.Empty;
    public string SessionName { get; set; } = string.Empty;
}

public class DiagnosticQueryRequest
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
}

public class DiagnosticQueryResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<Dictionary<string, object?>> Results { get; set; } = new();
    public List<string> ColumnNames { get; set; } = new();
    public int RecordCount { get; set; }
    public double ExecutionTimeMs { get; set; }
}

public class PredefinedQuery
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
}

public class ExtendedEventsRequest
{
    public string ConnectionString { get; set; } = string.Empty;
}
