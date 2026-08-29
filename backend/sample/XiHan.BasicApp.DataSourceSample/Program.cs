// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SqlSugar;
using XiHan.BasicApp.DataSourceSample;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Core.Extensions.Hosting;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Initializers;
using XiHan.Framework.Data.SqlSugar.Routing;
using XiHan.Framework.Domain.Repositories;

// 每次从干净的库开始，便于反复验证
foreach (var file in new[] { "default.db", "erp.db", "mes.db" })
{
    if (File.Exists(file))
    {
        File.Delete(file);
    }
}

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Warning);
await builder.Services.AddApplicationAsync<SampleModule>();

var host = builder.Build();
await host.InitializeAsync();

using var scope = host.Services.CreateScope();
var sp = scope.ServiceProvider;
var dataSourceResolver = sp.GetRequiredService<IEntityDataSourceResolver>();
var clientResolver = sp.GetRequiredService<ISqlSugarClientResolver>();

var failures = new List<string>();
void Check(string title, string expected, string actual)
{
    var ok = expected == actual;
    Console.WriteLine($"  {(ok ? "通过" : "失败")}  {title}：期望 {expected}，实际 {actual}");
    if (!ok)
    {
        failures.Add(title);
    }
}

Console.WriteLine();
Console.WriteLine("一、实体声明的数据源解析");
Check("SampleUser 未声明", "(未声明)", dataSourceResolver.ResolveConfigId(typeof(SampleUser)) ?? "(未声明)");
Check("ErpOrder 标 [DataSource]", "Erp", dataSourceResolver.ResolveConfigId(typeof(ErpOrder)) ?? "(未声明)");
Check("MesTask 标原生 [Tenant]", "Mes", dataSourceResolver.ResolveConfigId(typeof(MesTask)) ?? "(未声明)");

Console.WriteLine();
Console.WriteLine("二、仓储实际拿到的连接");
static string ConfigIdOf(ISqlSugarClient client) => client.CurrentConnectionConfig.ConfigId?.ToString() ?? "(空)";
Check("SampleUser 的客户端", "Default", ConfigIdOf(clientResolver.GetClientForEntity(typeof(SampleUser))));
Check("ErpOrder 的客户端", "Erp", ConfigIdOf(clientResolver.GetClientForEntity(typeof(ErpOrder))));
Check("MesTask 的客户端", "Mes", ConfigIdOf(clientResolver.GetClientForEntity(typeof(MesTask))));

Console.WriteLine();
Console.WriteLine("三、建表归属（启动时已按此建表）");
var entityTypeProvider = sp.GetRequiredService<IDbEntityTypeProvider>();
static string Names(IEnumerable<Type> types) => string.Join(" ", types.Select(t => t.Name).OrderBy(n => n));
foreach (var (configId, expected) in new[] { ("Default", "SampleUser"), ("Erp", "ErpOrder"), ("Mes", "MesTask") })
{
    var context = new DbInitializationContext(configId, null, isTenantDatabase: false);
    Check($"{configId} 库参与建表的实体", expected, Names(entityTypeProvider.GetEntityTypes(context)));
}

Console.WriteLine();
Console.WriteLine("四、写入后各库的物理内容（另开连接直读文件）");
var userRepository = sp.GetRequiredService<IRepositoryBase<SampleUser, long>>();
var orderRepository = sp.GetRequiredService<IRepositoryBase<ErpOrder, long>>();
var taskRepository = sp.GetRequiredService<IRepositoryBase<MesTask, long>>();

await userRepository.AddAsync(new SampleUser { UserName = "张三" });
await orderRepository.AddAsync(new ErpOrder { OrderNo = "ERP-0001" });
await taskRepository.AddAsync(new MesTask { TaskNo = "MES-0001" });

static string TablesIn(string file)
{
    using var db = new SqlSugarClient(new ConnectionConfig
    {
        ConnectionString = $"DataSource={file}",
        DbType = DbType.Sqlite,
        IsAutoCloseConnection = true
    });
    var names = db.DbMaintenance.GetTableInfoList(false)
        .Select(t => t.Name)
        .Where(n => !n.StartsWith("sqlite_", StringComparison.OrdinalIgnoreCase))
        .OrderBy(n => n, StringComparer.Ordinal);
    return string.Join(" ", names.Select(n => $"{n}({db.Ado.GetInt($"select count(*) from {n}")})"));
}

Check("default.db 的表(行数)", "sample_user(1)", TablesIn("default.db"));
Check("erp.db 的表(行数)", "erp_order(1)", TablesIn("erp.db"));
Check("mes.db 的表(行数)", "mes_task(1)", TablesIn("mes.db"));

Console.WriteLine();
Console.WriteLine("五、fail-closed：声明的 ConfigId 没有对应连接");
try
{
    clientResolver.GetClientForEntity(typeof(OrphanEntity));
    Check("应抛异常而非回落默认库", "抛异常", "未抛异常");
}
catch (Exception ex)
{
    Check("应抛异常而非回落默认库", "抛异常", "抛异常");
    Console.WriteLine($"        {ex.Message}");
}

Console.WriteLine();
if (failures.Count == 0)
{
    Console.WriteLine("全部通过。");
    return 0;
}

Console.WriteLine($"有 {failures.Count} 项未通过：{string.Join("、", failures)}");
return 1;
