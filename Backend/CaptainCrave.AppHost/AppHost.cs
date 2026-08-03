var builder = DistributedApplication.CreateBuilder(args);

var database = builder.AddConnectionString("DefaultConnection");

var api = builder
    .AddProject<Projects.CaptainCrave_Api>("api")
    .WithReference(database);


builder
    .AddViteApp("Client", "../../Client")
    .WithRunScript("start")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.Build().Run();