// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Auth;
using XiHan.Framework.Data.SqlSugar.Auditing;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Data.SqlSugar.Extensions;
using XiHan.Framework.Data.SqlSugar.Options;
using XiHan.Framework.DistributedIds;
using XiHan.Framework.DistributedIds.SnowflakeIds;
using XiHan.Framework.MultiTenancy;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 登录用户存储的账号域口径测试（真实 SQLite + 生产同款过滤器与插入审计）
/// </summary>
/// <remarks>
/// 平台就是 0 号租户：匿名登录请求没有租户上下文，只看得到平台行。账号与安全信息却打着归属租户的戳——
/// 用户存储必须跨租户定位账号、切入归属租户读写安全信息，否则租户用户一个都登不进来，
/// 失败计数、锁定与重置也会静默落空（表达式更新只命中 0 号行）。
/// </remarks>
public sealed class SaasUserStoreAccountScopeSqlTests : IDisposable
{
    private const string ConfigId = "Main";
    private const long HomeTenantId = 5;
    private const string Email = "user@acme.test";

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-user-store-{Guid.NewGuid():N}.db");
    private readonly ServiceProvider _services;
    private readonly SqlSugarScope _scope;
    private readonly ICurrentTenant _currentTenant;
    private readonly SaasUserStore _store;
    private readonly long _userId;

    /// <summary>
    /// 建库、按生产口径装配过滤器与插入审计，并在归属租户内播一个账号与它的安全信息
    /// </summary>
    public SaasUserStoreAccountScopeSqlTests()
    {
        AsyncLocalCurrentTenantAccessor.Instance.Current = null;

        var serviceCollection = new ServiceCollection();
        _ = serviceCollection.AddSingleton<ICurrentTenantAccessor>(AsyncLocalCurrentTenantAccessor.Instance);
        _ = serviceCollection.AddTransient<ICurrentTenant, CurrentTenant>();
        _services = serviceCollection.BuildServiceProvider();
        _currentTenant = _services.GetRequiredService<ICurrentTenant>();

        var configurator = new SqlSugarConnectionConfigurator(
            Options.Create(new XiHanSqlSugarCoreOptions()),
            AsyncLocalCurrentTenantAccessor.Instance,
            _services.GetRequiredService<IServiceScopeFactory>(),
            new SqlSugarDataExecutingHandler(
                _services.GetRequiredService<IServiceScopeFactory>(),
                IdGeneratorFactory.CreateSnowflakeIdGenerator(new SnowflakeIdOptions())));

        _scope = new SqlSugarScope(
            new ConnectionConfig
            {
                ConfigId = ConfigId,
                ConnectionString = $"DataSource={_databasePath};Pooling=False",
                DbType = DbType.Sqlite,
                IsAutoCloseConnection = true,
                MoreSettings = new ConnMoreSettings
                {
                    IsAutoUpdateQueryFilter = true,
                    IsAutoDeleteQueryFilter = true
                }
            },
            client => configurator.Configure(client.GetConnectionScope(ConfigId)));

        var client = _scope.GetConnectionScope(ConfigId);
        client.CodeFirst.InitTables<SysUser, SysUserSecurity>();

        using (_currentTenant.Change(HomeTenantId))
        {
            var user = client.Insertable(new SysUser { UserName = "user", Email = Email }).ExecuteReturnEntity();
            _userId = user.BasicId;
            _ = client.Insertable(new SysUserSecurity { UserId = _userId, Password = "hash", FailedLoginAttempts = 0 }).ExecuteCommand();
        }

        _store = new SaasUserStore(new FixedClientResolver(client), _currentTenant);
    }

    /// <summary>
    /// 平台作用域（匿名登录）按邮箱定位到归属租户的账号，并读到它的密码哈希
    /// </summary>
    [Fact]
    public async Task GetUserByUsername_InPlatformScope_ShouldLocateTenantAccountWithSecurity()
    {
        var user = await _store.GetUserByUsernameAsync(Email);

        Assert.NotNull(user);
        Assert.Equal(_userId.ToString(), user!.UserId);
        Assert.Equal("hash", user.PasswordHash);
        Assert.Null(_currentTenant.Id);
    }

    /// <summary>
    /// 平台作用域里累加与重置失败次数都真正落到归属租户的安全信息行
    /// </summary>
    [Fact]
    public async Task FailedAttempts_InPlatformScope_ShouldIncrementAndResetTenantSecurityRow()
    {
        await _store.IncrementFailedLoginAttemptsAsync(Email);
        await _store.IncrementFailedLoginAttemptsAsync(Email);
        Assert.Equal(2, await _store.GetFailedLoginAttemptsAsync(Email));
        Assert.Equal(2, LoadSecurity().FailedLoginAttempts);

        await _store.ResetFailedLoginAttemptsAsync(Email);

        Assert.Equal(0, LoadSecurity().FailedLoginAttempts);
        Assert.Null(_currentTenant.Id);
    }

    /// <summary>
    /// 按主键读取账号（刷新令牌等路径）不受当前作用域影响
    /// </summary>
    [Fact]
    public async Task GetUserById_InOtherTenantScope_ShouldStillLocateAccount()
    {
        using var otherTenant = _currentTenant.Change(HomeTenantId + 1);

        var user = await _store.GetUserByIdAsync(_userId.ToString());

        Assert.NotNull(user);
        Assert.Equal("hash", user!.PasswordHash);
        Assert.Equal(HomeTenantId + 1, _currentTenant.Id);
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public void Dispose()
    {
        AsyncLocalCurrentTenantAccessor.Instance.Current = null;
        _scope.Dispose();
        _services.Dispose();
        SaasTestHelper.DeleteTemporaryDatabase(_databasePath);
    }

    private SysUserSecurity LoadSecurity()
    {
        return _scope.GetConnectionScope(ConfigId)
            .Queryable<SysUserSecurity>()
            .ClearTenantFilter()
            .First(security => security.UserId == _userId);
    }

    /// <summary>固定返回同一客户端的解析器替身。</summary>
    private sealed class FixedClientResolver(ISqlSugarClient client) : ISqlSugarClientResolver
    {
        public ISqlSugarClient GetCurrentClient() => client;

        public ISqlSugarClient GetClientForEntity(Type entityType) => client;

        public ISqlSugarClient GetClient(string configId) => client;

        public IReadOnlyCollection<string> GetAllConfigIds() => [ConfigId];

        public IReadOnlyList<string> GetCurrentLayoutConfigIds() => [ConfigId];

        public IEnumerable<ISqlSugarClient> GetAllClients() => [client];

        public ITenant AsTenant() => throw new NotSupportedException("用例不涉及多库切换。");
    }
}
