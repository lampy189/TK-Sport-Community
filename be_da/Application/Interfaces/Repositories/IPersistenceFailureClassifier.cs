namespace TKSC.Application.Interfaces.Repositories;
public enum PersistenceFailure
{
    OutsideAssignedCenter,
    ReservedStockConflict,
    RevenueReconciliationRequired
}

public interface IPersistenceFailureClassifier
{
    PersistenceFailure? Classify(Exception error);
}
