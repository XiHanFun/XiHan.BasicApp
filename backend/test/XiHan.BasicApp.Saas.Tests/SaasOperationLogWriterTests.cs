// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Moq;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Logging;
using XiHan.Framework.Auditing;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 操作日志写入器测试（真实 SQLite 按月分表落库）。
/// </summary>
/// <remarks>
/// 回归锚点：导出中心轮询 <c>POST /api/ExportTaskQuery/Mine</c>（查询服务的 <c>GetMineAsync</c>），
/// 动作名被动态 API 剥成 <c>Mine</c>，写入器按动作名认不出语义、回退到 POST 记成「新增」，灌满操作日志。
/// 查询服务的端点只读，不论动词与动作名都不该进操作日志；命令服务照常按语义归类落库。
/// </remarks>
public sealed class SaasOperationLogWriterTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-operation-log-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _client;
    private readonly SaasOperationLogWriter _writer;

    /// <summary>
    /// 建库、建当月操作日志分表，装配写入器。
    /// </summary>
    public SaasOperationLogWriterTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });
        _client.CodeFirst.SplitTables().InitTables<SysOperationLog>();

        var clientInfoProvider = new Mock<IClientInfoProvider>();
        clientInfoProvider.Setup(provider => provider.GetCurrent()).Returns(new ClientInfo());

        _writer = new SaasOperationLogWriter(
            new FixedClientResolver(_client),
            new TestCurrentTenant(),
            clientInfoProvider.Object,
            new HttpContextAccessor());
    }

    /// <summary>
    /// 查询服务上走 POST 的读端点不落操作日志：此前它们按动作名分别被误记成新增、审核、修改。
    /// </summary>
    /// <param name="controllerName">动态 API 控制器名。</param>
    /// <param name="actionName">动态 API 动作名。</param>
    [Theory]
    [InlineData("ExportTaskQuery", "Mine")]
    [InlineData("TraceQuery", "TraceTimeline")]
    [InlineData("ReviewQuery", "ReviewPage")]
    [InlineData("PermissionChangeLogQuery", "PermissionChangeLogPage")]
    public async Task WriteAsync_QueryServicePostEndpoint_ShouldNotPersist(string controllerName, string actionName)
    {
        await _writer.WriteAsync(Record(controllerName, actionName, HttpMethods.Post));

        Assert.Empty(ReadOperationLogs());
    }

    /// <summary>
    /// 认证类动作仍由登录日志承担，不重复落操作日志。
    /// </summary>
    [Fact]
    public async Task WriteAsync_AuthLogin_ShouldNotPersist()
    {
        await _writer.WriteAsync(Record("Auth", "Login", HttpMethods.Post));

        Assert.Empty(ReadOperationLogs());
    }

    /// <summary>
    /// 命令服务的写端点照常落库，操作类型按动作语义归类，动作名认不出时才看 HTTP 方法。
    /// </summary>
    /// <param name="controllerName">动态 API 控制器名。</param>
    /// <param name="actionName">动态 API 动作名。</param>
    /// <param name="httpMethod">HTTP 方法。</param>
    /// <param name="expected">应落的操作类型。</param>
    [Theory]
    [InlineData("Role", "Role", "POST", OperationType.Create)]
    [InlineData("Role", "Role", "PUT", OperationType.Update)]
    [InlineData("Role", "SetRoleDataScope", "POST", OperationType.Update)]
    [InlineData("ExportTask", "Delete", "DELETE", OperationType.Delete)]
    public async Task WriteAsync_CommandServiceEndpoint_ShouldPersistWithSemanticType(
        string controllerName,
        string actionName,
        string httpMethod,
        OperationType expected)
    {
        await _writer.WriteAsync(Record(controllerName, actionName, httpMethod));

        var log = Assert.Single(ReadOperationLogs());
        Assert.Equal(expected, log.OperationType);
        Assert.Equal(controllerName, log.Module);
        Assert.Equal(actionName, log.Function);
        Assert.Equal($"{controllerName}.{actionName}", log.Title);
    }

    /// <summary>
    /// 释放连接并清理临时库文件。
    /// </summary>
    public void Dispose()
    {
        _client.Ado.Connection.Close();
        _client.Dispose();
        SaasTestHelper.DeleteTemporaryDatabase(_databasePath);
    }

    private List<SysOperationLog> ReadOperationLogs()
    {
        return _client.Queryable<SysOperationLog>().SplitTable().ToList();
    }

    private static OperationLogRecord Record(string controllerName, string actionName, string httpMethod)
    {
        return new OperationLogRecord
        {
            TraceId = "trace-operation-log",
            UserId = 1001,
            TenantId = 0,
            UserName = "admin",
            ControllerName = controllerName,
            ActionName = actionName,
            Method = httpMethod,
            Path = $"/api/{controllerName}/{actionName}",
            StatusCode = StatusCodes.Status200OK,
            ElapsedMilliseconds = 12
        };
    }

    /// <summary>
    /// 固定返回同一客户端的解析器替身。
    /// </summary>
    private sealed class FixedClientResolver(ISqlSugarClient client) : ISqlSugarClientResolver
    {
        /// <summary>
        /// 获取当前客户端。
        /// </summary>
        public ISqlSugarClient GetCurrentClient() => client;

        /// <summary>
        /// 获取实体对应的客户端。
        /// </summary>
        public ISqlSugarClient GetClientForEntity(Type entityType) => client;

        /// <summary>
        /// 按 ConfigId 获取指定客户端。
        /// </summary>
        public ISqlSugarClient GetClient(string configId) => client;

        /// <summary>
        /// 获取全部连接配置标识。
        /// </summary>
        public IReadOnlyCollection<string> GetAllConfigIds() => [];

        /// <summary>
        /// 获取当前布局的全部连接配置标识。
        /// </summary>
        public IReadOnlyList<string> GetCurrentLayoutConfigIds() => [];

        /// <summary>
        /// 获取所有客户端。
        /// </summary>
        public IEnumerable<ISqlSugarClient> GetAllClients() => [client];

        /// <summary>
        /// 底层 SqlSugarScope。
        /// </summary>
        public ITenant AsTenant() => throw new NotSupportedException();
    }
}
