using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.Stateless = true;
    })
    .WithMcpEndpoint("/domain1/mcp", McpServerTool.Create(Domain1Tools.A1), McpServerTool.Create(Domain1Tools.A2))
    .WithMcpEndpoint("/domain2/mcp", McpServerTool.Create(Domain2Tools.B1));

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.MapMcpEndpoint();

app.Run("https://localhost:3001");
