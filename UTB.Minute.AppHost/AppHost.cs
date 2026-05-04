using Microsoft.Extensions.Hosting;
var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres;

// Add PostgreSQL database with environment-specific configuration
if (builder.Environment.IsEnvironment("Testing"))
{
    postgres = builder.AddPostgres("postgres-testing")
                      .WithContainerName("postgres-testing-UTB.Minute");
}
else
{
    postgres = builder.AddPostgres("postgres")
                     .WithContainerName("postgres-UTB.Minute")
                     .WithPgAdmin() // Add pgAdmin for browsing DB data
                     .WithDataVolume() // This method allows saving data on a disk
                     .WithLifetime(ContainerLifetime.Persistent);
}

var database = postgres.AddDatabase("database");

// Add WebAPI with PostgreSQL reference
var webApi = builder.AddProject<Projects.UTB_Minute_WebApi>("webapi")
    .WithHttpHealthCheck("/health")
    .WithReference(database)
    .WaitFor(database);

// Add CanteenClient (for students and cooks)
builder.AddProject<Projects.UTB_Minute_CanteenClient>("canteenclient")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(webApi)
    .WaitFor(webApi);

// Add AdminClient (for canteen management)
builder.AddProject<Projects.UTB_Minute_AdminClient>("adminclient")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(webApi)
    .WaitFor(webApi);

// Add DbManager for database management
builder.AddProject<Projects.UTB_Minute_DbManager>("dbmanager")
    .WithReference(database)
    .WithHttpCommand("reset-database", "Reset database")
    .WaitFor(postgres);

builder.Build().Run();
