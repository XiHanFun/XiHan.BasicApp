// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Infrastructure.Repositories;
using XiHan.Framework.Data.SqlSugar.Clients;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 个人中心用户自有行的跨租户读取测试（真实 SQLite + 与生产同口径的租户读共享过滤器）。
/// </summary>
/// <remarks>
/// <para>
/// 跨租户成员（归属租户 1）切进租户 2 后，账号 / 安全记录 / 会话 / 三方绑定 / 偏好 / 设置 / 凭证
/// 这些按 UserId 归属的行带的仍是租户 1（或产生时所在租户）的戳，经全局租户过滤会整体不可见。
/// 每条用例先用带过滤的常规读证明过滤器确实生效，再断言个人中心用的读法能取到。
/// </para>
/// </remarks>
public sealed class ProfileUserOwnedRowsSqlTests : IDisposable
{
    private const long HomeTenantId = 1;
    private const long ActiveTenantId = 2;
    private const long UserId = 1001;
    private const long OtherUserId = 2002;
    private const long PlatformTenantScopeSentinel = long.MinValue;

    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-profile-owned-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _client;
    private readonly TestCurrentTenant _currentTenant = new();
    private readonly FixedClientResolver _resolver;

    /// <summary>
    /// 建库、挂租户读共享 + 软删过滤器。
    /// </summary>
    public ProfileUserOwnedRowsSqlTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = true
        });
        _client.QueryFilter.AddTableFilter<ISoftDelete>(entity => !entity.IsDeleted);
        _client.QueryFilter.AddTableFilter<IMultiTenantEntity>(
            entity => ResolveTenantScopeId() == PlatformTenantScopeSentinel ||
                      entity.TenantId == 0 ||
                      entity.TenantId == ResolveTenantScopeId());
        _client.CodeFirst.InitTables<SysUserSession>();
        _client.CodeFirst.InitTables<SysUserSecurity>();
        _client.CodeFirst.InitTables<SysExternalLogin>();
        _client.CodeFirst.InitTables<SysUserNotificationPreference>();
        _client.CodeFirst.InitTables<SysUserSetting>();
        _client.CodeFirst.InitTables<SysUserApiCredential>();

        _resolver = new FixedClientResolver(_client);
    }

    /// <summary>
    /// 会话：自己在各租户戳下的会话 + 由自己发起的模仿会话都取到；已吊销的与别人的不取。
    /// </summary>
    [Fact]
    public async Task GetNotRevokedByUserIgnoreTenantAsync_ShouldReturnOwnAndImpersonatedSessionsAcrossTenants()
    {
        Insert(Session(UserId, "home-phone", HomeTenantId), 1);
        Insert(Session(UserId, "active-pc", ActiveTenantId), 2);
        Insert(Session(OtherUserId, "impersonation", ActiveTenantId, impersonatorUserId: UserId), 3);
        Insert(Session(UserId, "revoked", HomeTenantId, SessionStatus.Revoked), 4);
        Insert(Session(OtherUserId, "someone-else", HomeTenantId), 5);
        var repository = new UserSessionRepository(_resolver);

        using var tenantScope = _currentTenant.Change(ActiveTenantId);

        // 带租户过滤的常规读只看得到本租户戳的行
        var filtered = await repository.GetActiveSessionsAsync(UserId);
        Assert.Equal(["active-pc"], filtered.Select(session => session.UserSessionId));

        var owned = await repository.GetNotRevokedByUserIgnoreTenantAsync(UserId);
        Assert.Equal(
            ["active-pc", "home-phone", "impersonation"],
            owned.Select(session => session.UserSessionId).Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// 安全记录：唯一键是 UserId，在别的租户里也要能按用户取到。
    /// </summary>
    [Fact]
    public async Task UserSecurity_GetByUserIdAsync_ShouldIgnoreTenantFilter()
    {
        Insert(new SysUserSecurity { UserId = UserId, TenantId = HomeTenantId, Password = "hashed" }, 1);
        var repository = new UserSecurityRepository(_resolver);

        using var tenantScope = _currentTenant.Change(ActiveTenantId);

        Assert.Null(await repository.GetByIdAsync(1));
        Assert.NotNull(await repository.GetByUserIdAsync(UserId));
    }

    /// <summary>
    /// 三方绑定：按用户跨租户取全，别人的不取。
    /// </summary>
    [Fact]
    public async Task ExternalLogin_GetListByUserIdIgnoreTenantAsync_ShouldReturnAllBindingsOfUser()
    {
        Insert(new SysExternalLogin { UserId = UserId, TenantId = HomeTenantId, Provider = "github", ProviderKey = "g-1" }, 1);
        Insert(new SysExternalLogin { UserId = UserId, TenantId = ActiveTenantId, Provider = "gitee", ProviderKey = "e-1" }, 2);
        Insert(new SysExternalLogin { UserId = OtherUserId, TenantId = HomeTenantId, Provider = "github", ProviderKey = "g-2" }, 3);
        var repository = new ExternalLoginRepository(_resolver);

        using var tenantScope = _currentTenant.Change(ActiveTenantId);

        Assert.Null(await repository.GetByIdAsync(1));
        var bindings = await repository.GetListByUserIdIgnoreTenantAsync(UserId);
        Assert.Equal(["gitee", "github"], bindings.Select(binding => binding.Provider).Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// 通知偏好 / 用户设置：唯一键不含租户，在别的租户里也要能按键取到，否则会再插一行撞唯一索引。
    /// </summary>
    [Fact]
    public async Task PreferenceAndSetting_GetByKey_ShouldIgnoreTenantFilter()
    {
        Insert(new SysUserNotificationPreference { UserId = UserId, TenantId = HomeTenantId }, 1);
        Insert(new SysUserSetting { UserId = UserId, TenantId = HomeTenantId, Scene = UserSettingScene.Preference, SettingKey = "theme", SettingValue = "dark" }, 1);
        var preferenceRepository = new UserNotificationPreferenceRepository(_resolver);
        var settingRepository = new UserSettingRepository(_resolver);

        using var tenantScope = _currentTenant.Change(ActiveTenantId);

        Assert.Null(await preferenceRepository.GetByIdAsync(1));
        Assert.NotNull(await preferenceRepository.GetByUserIdAsync(UserId));

        Assert.Null(await settingRepository.GetByIdAsync(1));
        var setting = await settingRepository.GetByUserSettingAsync(UserId, UserSettingScene.Preference, "theme");
        Assert.Equal("dark", setting?.SettingValue);
    }

    /// <summary>
    /// 接口凭证：按人跨租户取全，软删的不取。
    /// </summary>
    [Fact]
    public async Task ApiCredential_GetListByUserIdAsync_ShouldReturnCredentialsAcrossTenantsExceptDeleted()
    {
        Insert(new SysUserApiCredential { UserId = UserId, TenantId = HomeTenantId, CredentialName = "home", AppKey = "ak_home", SecretCipher = "c" }, 1);
        Insert(new SysUserApiCredential { UserId = UserId, TenantId = ActiveTenantId, CredentialName = "active", AppKey = "ak_active", SecretCipher = "c" }, 2);
        Insert(new SysUserApiCredential { UserId = UserId, TenantId = HomeTenantId, CredentialName = "deleted", AppKey = "ak_deleted", SecretCipher = "c", IsDeleted = true }, 3);
        Insert(new SysUserApiCredential { UserId = OtherUserId, TenantId = HomeTenantId, CredentialName = "other", AppKey = "ak_other", SecretCipher = "c" }, 4);
        var repository = new UserApiCredentialRepository(_resolver);

        using var tenantScope = _currentTenant.Change(ActiveTenantId);

        Assert.Null(await repository.GetByIdAsync(1));
        var credentials = await repository.GetListByUserIdAsync(UserId);
        Assert.Equal(["ak_active", "ak_home"], credentials.Select(credential => credential.AppKey).Order(StringComparer.Ordinal));
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

    private long ResolveTenantScopeId()
    {
        return _currentTenant.Id ?? PlatformTenantScopeSentinel;
    }

    private static SysUserSession Session(
        long userId,
        string sessionId,
        long tenantId,
        SessionStatus status = SessionStatus.Active,
        long? impersonatorUserId = null)
    {
        return new SysUserSession
        {
            UserId = userId,
            ImpersonatorUserId = impersonatorUserId,
            UserSessionId = sessionId,
            TenantId = tenantId,
            Status = status,
            LoginTime = DateTimeOffset.UnixEpoch,
            LastActivityTime = DateTimeOffset.UnixEpoch
        };
    }

    private void Insert<TEntity>(TEntity entity, long basicId) where TEntity : class, new()
    {
        SaasTestHelper.SetBasicId(entity, basicId);
        _ = _client.Insertable(entity).ExecuteCommand();
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
