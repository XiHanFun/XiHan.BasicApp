// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.AccessPolicies;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Web.Core.Clients;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 访问策略测试装配：真实的编译器、求值器与属性来源，快照、时钟、用户与角色可替换
/// </summary>
internal static class AccessPolicyTestKit
{
    /// <summary>
    /// 默认时刻：2026-10-22 周四 10:00（Asia/Shanghai）
    /// </summary>
    public static readonly DateTimeOffset Now = new(2026, 10, 22, 2, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// 条件编译器
    /// </summary>
    public static AccessPolicyConditionCompiler Compiler()
    {
        return new AccessPolicyConditionCompiler(
            new DefaultConditionCompiler(new DefaultConditionAttributeRegistry(SaasConditionAttributes.All)),
            Options.Create(new SaasConditionOptions()));
    }

    /// <summary>
    /// 一层策略的求值
    /// </summary>
    public static AccessPolicyEvaluator Evaluator()
    {
        return new AccessPolicyEvaluator(new DefaultConditionEvaluator());
    }

    /// <summary>
    /// 条件
    /// </summary>
    public static AccessPolicyConditionCommand Clause(string attribute, ConditionOperator @operator, string value, int group = 0, bool isNegated = false)
    {
        return new AccessPolicyConditionCommand(group, attribute, @operator, isNegated, ConfigDataType.String, value, null);
    }

    /// <summary>
    /// 编译好的策略
    /// </summary>
    public static CompiledAccessPolicy Policy(
        long id,
        long tenantId,
        AccessPolicyAction action,
        params AccessPolicyConditionCommand[] clauses)
    {
        return Policy(id, tenantId, action, null, null, clauses);
    }

    /// <summary>
    /// 编译好的策略（带生效期）
    /// </summary>
    public static CompiledAccessPolicy Policy(
        long id,
        long tenantId,
        AccessPolicyAction action,
        DateTimeOffset? effectiveTime,
        DateTimeOffset? expirationTime,
        params AccessPolicyConditionCommand[] clauses)
    {
        var compiled = Compiler().Compile(clauses);
        Assert.Empty(compiled.Errors);
        return new CompiledAccessPolicy(id, tenantId, $"policy-{id}", $"策略 {id}", action, (int)id, effectiveTime, expirationTime, compiled);
    }

    /// <summary>
    /// 选项
    /// </summary>
    public static PermissionConditionTestKit.FixedOptionsMonitor<AccessPolicyOptions> PolicyOptions(bool enabled = true)
    {
        return new PermissionConditionTestKit.FixedOptionsMonitor<AccessPolicyOptions>(new AccessPolicyOptions { Enabled = enabled });
    }

    /// <summary>
    /// 命中记录器
    /// </summary>
    public static AccessPolicyHitRecorder Hits(ManualTimeProvider time, int capacity = 10_000)
    {
        return new AccessPolicyHitRecorder(
            new PermissionConditionTestKit.FixedOptionsMonitor<AccessPolicyOptions>(new AccessPolicyOptions { HitBufferCapacity = capacity }),
            time,
            NullLogger<AccessPolicyHitRecorder>.Instance);
    }

    /// <summary>
    /// 带客户端地址的请求
    /// </summary>
    public static DefaultHttpContext Request(string ip, string path = "/api/Demo/Get", string method = "GET")
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        context.Request.Path = path;
        context.Request.Method = method;
        context.Response.Body = new MemoryStream();
        return context;
    }

    /// <summary>
    /// 可手动拨动的时钟
    /// </summary>
    public sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
    {
        /// <summary>
        /// 当前时刻
        /// </summary>
        public DateTimeOffset Now { get; set; } = now;

        /// <inheritdoc />
        public override DateTimeOffset GetUtcNow()
        {
            return Now;
        }
    }

    /// <summary>
    /// 返回固定快照的提供器
    /// </summary>
    public sealed class FixedSnapshots(params CompiledAccessPolicy[] policies) : IAccessPolicySnapshotProvider
    {
        /// <summary>
        /// 当前快照
        /// </summary>
        public AccessPolicySnapshot Snapshot { get; set; } = new(policies, null);

        /// <summary>
        /// 取快照时抛出的异常
        /// </summary>
        public Exception? Failure { get; set; }

        /// <summary>
        /// 失效次数
        /// </summary>
        public int Invalidations { get; private set; }

        /// <inheritdoc />
        public ValueTask<AccessPolicySnapshot> GetAsync(CancellationToken cancellationToken = default)
        {
            return Failure is null ? ValueTask.FromResult(Snapshot) : ValueTask.FromException<AccessPolicySnapshot>(Failure);
        }

        /// <inheritdoc />
        public Task InvalidateAsync(CancellationToken cancellationToken = default)
        {
            Invalidations++;
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// 属性来源工厂：真实的属性来源，角色按用户主键给定，当前用户与租户可设
    /// </summary>
    public sealed class Sources : ISaasConditionAttributeSourceFactory
    {
        private readonly SaasConditionSubjectLoader _loader;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Sources(IHttpContextAccessor? httpContextAccessor = null)
        {
            HttpContextAccessor = httpContextAccessor ?? new HttpContextAccessor();
            var snapshots = new Mock<IAuthorizationSnapshotQueryService>();
            snapshots
                .Setup(service => service.BuildAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((long userId, DateTimeOffset _, CancellationToken _) =>
                    new AuthorizationSnapshot(Roles.GetValueOrDefault(userId) ?? [], [], [], new HashSet<string>(StringComparer.OrdinalIgnoreCase)));
            _loader = new SaasConditionSubjectLoader(
                snapshots.Object,
                new Mock<IUserDepartmentRepository>().Object,
                new Mock<IDepartmentRepository>().Object,
                new Mock<IPositionRepository>().Object,
                new Mock<ITenantUserRepository>().Object,
                new Mock<ITenantRepository>().Object,
                CurrentTenant);
        }

        /// <summary>
        /// 每个用户的角色编码
        /// </summary>
        public Dictionary<long, List<string>> Roles { get; } = [];

        /// <summary>
        /// 当前用户主键
        /// </summary>
        public long? UserId { get; set; }

        /// <summary>
        /// 当前租户
        /// </summary>
        public TestCurrentTenant CurrentTenant { get; } = new();

        /// <summary>
        /// 请求访问器
        /// </summary>
        public IHttpContextAccessor HttpContextAccessor { get; }

        /// <inheritdoc />
        public SaasConditionContext CreateCurrentContext(DateTimeOffset now)
        {
            return new SaasConditionContext
            {
                TenantId = CurrentTenant.Id ?? 0,
                Now = now,
                UserId = UserId,
                UserName = UserId is null ? null : $"user{UserId}",
                HttpContext = HttpContextAccessor.HttpContext
            };
        }

        /// <inheritdoc />
        public IConditionAttributeSource Create(SaasConditionContext context)
        {
            return new MemoizingConditionAttributeSource(new SaasConditionAttributeSource(context, _loader, new NullIpRegions(), new SaasConditionOptions().ResolveTimeZone()));
        }
    }

    /// <summary>
    /// 查不到任何归属地的解析器
    /// </summary>
    public sealed class NullIpRegions : IIpRegionResolver
    {
        /// <inheritdoc />
        public IpRegion? Resolve(IPAddress address)
        {
            return null;
        }
    }

    /// <summary>
    /// 固定的当前租户
    /// </summary>
    public static ICurrentTenant Tenant(long? tenantId)
    {
        return new TestCurrentTenant(tenantId);
    }
}
