namespace TKSC.Application.Interfaces.Repositories;
public enum PersistenceFailure
{
    OutsideAssignedCenter,
    RevenueReconciliationRequired
}

public interface IPersistenceFailureClassifier
{
    PersistenceFailure? Classify(Exception error);
}
