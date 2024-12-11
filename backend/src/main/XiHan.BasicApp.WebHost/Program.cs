using XiHan.BasicApp.WebHost;
using XiHan.Framework.AspNetCore.Extensions.Builder;
using XiHan.Framework.Core.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
await builder.Services.AddApplicationAsync<XiHanBasicAppWebHostModule>();

var app = builder.Build();

await app.InitializeApplicationAsync();

await app.RunAsync();
