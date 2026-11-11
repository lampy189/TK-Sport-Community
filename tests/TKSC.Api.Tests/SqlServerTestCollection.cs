using Xunit;

namespace TKSC.Api.Tests;

// Rollback fixtures still acquire real SQL locks; avoid concurrent test transactions.
[CollectionDefinition("SQL Server", DisableParallelization = true)]
public sealed class SqlServerTestCollection;
