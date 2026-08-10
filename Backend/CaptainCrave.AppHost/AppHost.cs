var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.CaptainCrave_Api>("api");

builder
    .AddViteApp("Client", "../../Client")
    .WithRunScript("start")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();