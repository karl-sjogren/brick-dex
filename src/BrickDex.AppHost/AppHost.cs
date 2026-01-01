var builder = DistributedApplication.CreateBuilder(args);

var sqlServer = builder.AddSqlServer("sqlserver")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDbGate();

var database = sqlServer.AddDatabase("brickdex");

builder.AddProject<Projects.BrickDex_Web>("brickdex-web")
    .WithReference(database)
    .WaitFor(database);

builder.Build().Run();
