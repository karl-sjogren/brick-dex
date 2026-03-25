var builder = DistributedApplication.CreateBuilder(args);

#pragma warning disable ASPIRECOMPUTE003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var registry = builder.AddContainerRegistry("ghcr", "ghcr.io", "karl-sjogren/brick-dex");
builder.AddDockerComposeEnvironment("brickdex");

var sqlServer = builder.AddSqlServer("brickdex-sqlserver")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDbGate()
    .PublishAsDockerComposeService((_, service) => {
        service.Name = "sqlserver";
        service.Restart = "unless-stopped";
    });

var database = sqlServer.AddDatabase("brickdexdb");

var web = builder.AddProject<Projects.BrickDex_Web>("brickdex-web")
    .WithReference(database)
    .WaitFor(database)
    .WithExternalHttpEndpoints()
    .WithUrlForEndpoint("http", u => u.DisplayLocation = UrlDisplayLocation.DetailsOnly)
    .PublishAsDockerFile(options => {
        options.WithDockerfile("../.."); // This can't have an ending slash
    })
    .PublishAsDockerComposeService((_, service) => {
        service.Name = "web";
        service.Restart = "unless-stopped";
    })
    .WithContainerRegistry(registry);

if(builder.ExecutionContext.IsRunMode) {
    var frontend = builder.AddViteApp("frontend", "../BrickDex.Frontend")
        .WithYarn()
        .WithRunScript("dev")
        .WithEndpoint("https", endpoint => endpoint.Port = 5010);

    web.WithReference(frontend);
}

builder.Build().Run();

#pragma warning restore ASPIRECOMPUTE003 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
