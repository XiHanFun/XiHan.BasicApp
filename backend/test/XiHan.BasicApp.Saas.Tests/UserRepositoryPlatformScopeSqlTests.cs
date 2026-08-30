// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 账号判重的连接落点测试（平台库 / 租户独立库两套真实 SQLite 连接）。
/// </summary>
/// <remarks>
/// <para>
/// 库隔离部署下当前租户上下文决定连接解析：租户上下文里发出的查询会打到该租户的独立库。
/// 账号注册表落在平台库（登录统一在平台态按全局唯一邮箱定位账号），所以"全平台判重"
/// 必须自己切到平台态执行，否则查的是租户库——租户库要么还没建（开通期报 3D000/42P01），
/// 要么是空的（判重恒为 false，全局唯一形同虚设）。
/// </para>
/// <para>
/// 两个库里放不同的账号，断言双向成立：平台库的账号查得到、租户库的账号查不到，
/// 从而钉死"查的是哪个库"，而不只是"返回值碰巧对"。
/// </para>
/// </remarks>
public sealed class UserRepositoryPlatformScopeSqlTests : IDisposable
{
    /// <summary>
    /// 被测租户主键
    /// </summary>
    private const long TenantId = 7;

    /// <summary>
    /// 另一个租户的主键
    /// </summary>
    private const long OtherTenantId = 8;

    private readonly string _platformDatabasePath = Path.Combine(Path.GetTempPath(), $"xihan-platform-{Guid.NewGuid():N}.db");
    private readonly string _tenantDatabasePath = Path.Combine(Path.GetTempPath(), $"xihan-tenant-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _platformClient;
    private readonly SqlSugarClient _tenantClient;
    private readonly TestCurrentTenant _currentTenant = new();
    private readonly UserRepository _repository;

    /// <summary>
    /// 建两套库并把生产仓储接到按租户上下文选库的解析器上。
    /// </summary>
    public UserRepositoryPlatformScopeSqlTests()
    {
        _platformClient = CreateClient(_platformDatabasePath);
        _tenantClient = CreateClient(_tenantDatabasePath);
        _platformClient.CodeFirst.InitTables<SysUser>();
        _tenantClient.CodeFirst.InitTables<SysUser>();

        // 平台库：账号注册表的真身
        InsertUser(_platformClient, basicId: 1, tenantId: TenantId, userName: "owner", email: "owner@example.com");
        InsertUser(_platformClient, basicId: 2, tenantId: OtherTenantId, userName: "admin", email: "admin@example.com");
        InsertUser(_platformClient, basicId: 3, tenantId: 0, userName: "root", email: "root@example.com");

        // 租户独立库：同名表里放一条平台库没有的账号，用来暴露"查错库"
        InsertUser(_tenantClient, basicId: 4, tenantId: TenantId, userName: "ghost", email: "ghost@example.com");

        _repository = new UserRepository(
            new TenantAwareClientResolver(_currentTenant, _platformClient, _tenantClient),
            new Mock<IUnitOfWorkManager>().Object,
            _currentTenant);
    }

    /// <summary>
    /// 租户上下文里发起的邮箱判重必须查平台库。
    /// </summary>
    [Fact]
    public async Task ExistsEmailGloballyAsync_InTenantContext_ShouldQueryPlatformDatabase()
    {
        using var tenantScope = _currentTenant.Change(TenantId, "库隔离租户");

        Assert.True(await _repository.ExistsEmailGloballyAsync("owner@example.com"));
        Assert.False(await _repository.ExistsEmailGloballyAsync("ghost@example.com"));
    }

    /// <summary>
    /// 判重跑完要还原调用方的租户上下文。
    /// </summary>
    [Fact]
    public async Task ExistsEmailGloballyAsync_ShouldRestoreCallerTenantContext()
    {
        using var tenantScope = _currentTenant.Change(TenantId, "库隔离租户");

        _ = await _repository.ExistsEmailGloballyAsync("owner@example.com");

        Assert.Equal(TenantId, _currentTenant.Id);
    }

    /// <summary>
    /// 排除自身后同一条账号不再算占用。
    /// </summary>
    [Fact]
    public async Task ExistsEmailGloballyAsync_WithExcludedUser_ShouldIgnoreThatRow()
    {
        using var tenantScope = _currentTenant.Change(TenantId, "库隔离租户");

        Assert.False(await _repository.ExistsEmailGloballyAsync("owner@example.com", excludeUserId: 1));
    }

    /// <summary>
    /// 用户名判重查平台库，范围限定在入参租户加平台账号。
    /// </summary>
    [Fact]
    public async Task ExistsUserNameInTenantAsync_ShouldScopeToGivenTenantAndPlatformAccounts()
    {
        using var tenantScope = _currentTenant.Change(TenantId, "库隔离租户");

        // 本租户已有
        Assert.True(await _repository.ExistsUserNameInTenantAsync(TenantId, "owner"));
        // 平台账号重名同样算占用
        Assert.True(await _repository.ExistsUserNameInTenantAsync(TenantId, "root"));
        // 别的租户的同名账号不算占用
        Assert.False(await _repository.ExistsUserNameInTenantAsync(TenantId, "admin"));
        // 租户独立库里的账号不参与判重
        Assert.False(await _repository.ExistsUserNameInTenantAsync(TenantId, "ghost"));
    }

    /// <summary>
    /// 平台态调用时租户范围同样来自入参，不受上下文缺省影响。
    /// </summary>
    [Fact]
    public async Task ExistsUserNameInTenantAsync_InPlatformContext_ShouldStillScopeByArgument()
    {
        Assert.True(await _repository.ExistsUserNameInTenantAsync(OtherTenantId, "admin"));
        Assert.False(await _repository.ExistsUserNameInTenantAsync(OtherTenantId, "owner"));
    }

    /// <summary>
    /// 释放连接并清理临时库文件。
    /// </summary>
    public void Dispose()
    {
        _platformClient.Ado.Connection.Close();
        _platformClient.Dispose();
        _tenantClient.Ado.Connection.Close();
        _tenantClient.Dispose();
        SaasTestHelper.DeleteTemporaryDatabase(_platformDatabasePath);
        SaasTestHelper.DeleteTemporaryDatabase(_tenantDatabasePath);
    }

    /// <summary>
    /// 建一个关闭连接池的临时 SQLite 客户端，避免用例结束后仍持有库文件句柄。
    /// </summary>
    private static SqlSugarClient CreateClient(string databasePath)
    {
        return new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });
    }

    /// <summary>
    /// 直接落一条账号，主键按 SqlSugar 的赋值方式回填。
    /// </summary>
    private static void InsertUser(ISqlSugarClient client, long basicId, long tenantId, string userName, string email)
    {
        var user = new SysUser
        {
            TenantId = tenantId,
            UserName = userName,
            Email = email,
            CreatedTime = DateTimeOffset.UnixEpoch
        };
        SaasTestHelper.SetBasicId(user, basicId);
        _ = client.Insertable(user).ExecuteCommand();
    }

    /// <summary>
    /// 按当前租户上下文选库的解析器替身：平台态取平台库，租户态取该租户的独立库。
    /// </summary>
    private sealed class TenantAwareClientResolver(
        TestCurrentTenant currentTenant,
        ISqlSugarClient platformClient,
        ISqlSugarClient tenantClient) : ISqlSugarClientResolver
    {
        /// <summary>
        /// 获取当前租户对应的客户端。
        /// </summary>
        public ISqlSugarClient GetCurrentClient() => currentTenant.Id is null ? platformClient : tenantClient;

        /// <summary>
        /// 获取实体对应的客户端。
        /// </summary>
        public ISqlSugarClient GetClientForEntity(Type entityType) => GetCurrentClient();

        /// <summary>
        /// 按 ConfigId 获取指定客户端。
        /// </summary>
        public ISqlSugarClient GetClient(string configId) => GetCurrentClient();

        /// <summary>
        /// 获取全部连接配置标识。
        /// </summary>
        public IReadOnlyCollection<string> GetAllConfigIds() => [];

        /// <summary>
        /// 获取当前布局的全部连接配置标识。
        /// </summary>
        public IReadOnlyList<string> GetCurrentLayoutConfigIds() => [];

        /// <summary>
        /// 按顺序获取所有库的客户端。
        /// </summary>
        public IEnumerable<ISqlSugarClient> GetAllClients() => [platformClient, tenantClient];

        /// <summary>
        /// 底层 SqlSugarScope。
        /// </summary>
        public ITenant AsTenant() => throw new NotSupportedException();
    }
}
