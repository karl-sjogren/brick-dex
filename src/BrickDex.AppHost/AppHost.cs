var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddAzureSqlServer("brickdex-sqlserver")
    .RunAsContainer(container => {
        container.WithDataVolume();
        container.WithLifetime(ContainerLifetime.Persistent);
        container.WithDbGate();
    });

var database = sqlServer.AddDatabase("brickdex");

var functions = builder.AddAzureFunctionsProject<Projects.BrickDex_Functions>("brickdex-functions")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints();

builder.AddProject<Projects.BrickDex_Web>("brickdex-web")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints();

builder.Build().Run();
