using Microsoft.Data.SqlClient;
using TKSC.Application.Interfaces.Repositories;

namespace TKSC.Infrastructure.Persistence;
public sealed class PersistenceFailureClassifier : IPersistenceFailureClassifier
{
    public PersistenceFailure? Classify(Exception error)
    {
        var sql = error as SqlException ?? error.InnerException as SqlException;
        return sql?.Number switch
        {
            33504 => PersistenceFailure.OutsideAssignedCenter,
            >= 51031 and <= 51034 => PersistenceFailure.RevenueReconciliationRequired,
            _ => null
        };
    }
}
