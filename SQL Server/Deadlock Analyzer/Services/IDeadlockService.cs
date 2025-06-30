using DeadlockAnalyzer.Models;

namespace DeadlockAnalyzer.Services;

public interface IDeadlockService
{
    Task<DatabaseConnectionResponse> TestConnectionAsync(DatabaseConnectionRequest request);
    Task<DeadlockEventsResponse> GetDeadlockEventsAsync(DeadlockEventsRequest request);
    Task<DiagnosticQueryResponse> ExecuteDiagnosticQueryAsync(DiagnosticQueryRequest request);
    Task<List<ExtendedEventSession>> GetExtendedEventSessionsAsync(string connectionString);
}
