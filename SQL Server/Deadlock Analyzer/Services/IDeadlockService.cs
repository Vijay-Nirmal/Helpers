using DeadlockAnalyzer.Models;

namespace DeadlockAnalyzer.Services;

public interface IDeadlockService
{
    Task<DeadlockEventsResponse> GetDeadlockEventsAsync(DeadlockEventsRequest request);
    Task<DiagnosticQueryResponse> ExecuteDiagnosticQueryAsync(DiagnosticQueryRequest request);
    List<PredefinedQuery> GetPredefinedQueries();
}
