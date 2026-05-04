using Xunit;

namespace UTB.Minute.WebApi.Tests;

/// <summary>
/// Collection definition for shared TestFixture
/// Ensures all tests in this collection use the same database instance
/// DisableParallelization prevents concurrent test execution which could interfere with shared database state
/// </summary>
[CollectionDefinition("Database collection", DisableParallelization = true)]
public class DatabaseCollection : ICollectionFixture<TestFixture>
{
}
