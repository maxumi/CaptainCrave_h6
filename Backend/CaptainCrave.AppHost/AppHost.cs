var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.CaptainCrave_Api>("api");

var client = builder
    .AddViteApp("Client", "../../Client")
    .WithRunScript("start")
    .WithReference(api)
    .WithExternalHttpEndpoints();

builder.AddExecutable(
        "playwright",
        "npx",
        "../../Client",
        "playwright",
        "test")
    .WithEnvironment(
        "PLAYWRIGHT_TEST_BASE_URL",
        client.GetEndpoint("http"))
    .WaitFor(client);

builder.Build().Run();