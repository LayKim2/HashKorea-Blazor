var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.HashKorea_Blazor>("hashkorea-blazor");

builder.Build().Run();
