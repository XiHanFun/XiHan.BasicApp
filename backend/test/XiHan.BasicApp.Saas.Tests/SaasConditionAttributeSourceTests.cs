// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Extensions;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Web.Api.Security.OpenApi;
using XiHan.Framework.Web.Core.Clients;
using FrameworkConditionOperator = XiHan.Framework.Authorization.Conditions.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 条件属性目录与属性来源：权限条件与访问策略共用的取值
/// </summary>
public sealed class SaasConditionAttributeSourceTests
{
    private const long UserId = 1001;
    private const long TenantId = 7;

    // 北京时间 2026-10-23（周五）01:30:45
    private static readonly DateTimeOffset Now = new(2026, 10, 22, 17, 30, 45, TimeSpan.Zero);

    private readonly TestCurrentTenant _currentTenant = new();
    private readonly Mock<IAuthorizationSnapshotQueryService> _snapshots = new();
    private readonly Mock<IUserDepartmentRepository> _userDepartments = new();
    private readonly Mock<IDepartmentRepository> _departments = new();
    private readonly Mock<IPositionRepository> _positions = new();
    private readonly Mock<ITenantUserRepository> _tenantUsers = new();
    private readonly Mock<ITenantRepository> _tenants = new();
    private readonly FakeIpRegionResolver _ipRegions = new();
    private readonly List<long?> _tenantSeenBySnapshot = [];

    /// <summary>
    /// 构造测试上下文
    /// </summary>
    public SaasConditionAttributeSourceTests()
    {
        _snapshots
            .Setup(service => service.BuildAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                _tenantSeenBySnapshot.Add(_currentTenant.Id);
                return new AuthorizationSnapshot(["clerk", "auditor"], [], [], new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            });

        _userDepartments
            .Setup(repository => repository.GetValidByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SysUserDepartment { UserId = UserId, DepartmentId = 11, PositionId = 21, IsMain = true },
                new SysUserDepartment { UserId = UserId, DepartmentId = 12, PositionId = 22 },
                // 部门已停用：任职与其岗位都不算
                new SysUserDepartment { UserId = UserId, DepartmentId = 13, PositionId = 23 }
            ]);
        _departments
            .Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Department(11, "sales"),
                Department(12, "finance"),
                Department(13, "legacy", EnableStatus.Disabled)
            ]);
        _positions
            .Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                Position(21, "manager"),
                Position(22, "clerk"),
                Position(23, "archivist")
            ]);
        _tenantUsers
            .Setup(repository => repository.GetMembershipAsync(TenantId, UserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SysTenantUser { UserId = UserId, MemberType = TenantMemberType.External });
        var tenant = new SysTenant { TenantCode = "acme", TenantName = "Acme" };
        SaasTestHelper.SetBasicId(tenant, TenantId);
        _tenants
            .Setup(repository => repository.GetByIdAsync(TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _ipRegions.Regions[IPAddress.Parse("114.114.114.114")] = new IpRegion("中国", "江苏省", "南京市", "电信", "CN");
    }

    /// <summary>
    /// 目录建得成登记表：旧名按别名解析到新名，路由值与查询串按族解析，数据行属性不在目录里
    /// </summary>
    [Theory]
    [InlineData("subject.role", "subject.roleCodes", true)]
    [InlineData("subject.department", "subject.departmentCodes", true)]
    [InlineData("resource.permissionCode", "resource.permissionCodes", true)]
    [InlineData("Environment.IP", "environment.ip", false)]
    [InlineData("resource.route.id", "resource.route.id", false)]
    [InlineData("resource.query.status", "resource.query.status", false)]
    public void Catalog_ResolvesAliasesAndFamilies(string name, string canonical, bool isAlias)
    {
        var registry = new DefaultConditionAttributeRegistry(SaasConditionAttributes.All);

        Assert.True(registry.TryResolve(name, out var resolution));
        Assert.Equal(canonical, resolution.CanonicalName, ignoreCase: true);
        Assert.Equal(isAlias, resolution.IsAlias);
    }

    /// <summary>
    /// 数据行属性由业务模块按权限码登记，目录里没有
    /// </summary>
    [Theory]
    [InlineData("resource.status")]
    [InlineData("resource.route")]
    [InlineData("environment.unknown")]
    public void Catalog_DoesNotKnowRowAttributes(string name)
    {
        var registry = new DefaultConditionAttributeRegistry(SaasConditionAttributes.All);

        Assert.False(registry.TryResolve(name, out _));
    }

    /// <summary>
    /// 时间类属性按配置的时区算：UTC 17:30 是北京次日 01:30、周五
    /// </summary>
    [Fact]
    public async Task Environment_TimeAttributes_UseConfiguredTimeZone()
    {
        var source = CreateSource(new SaasConditionContext { TenantId = TenantId, Now = Now });

        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentTime, new TimeOnly(1, 30));
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentHour, 1);
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentWeekday, 5);
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentDate, new DateOnly(2026, 10, 23));
    }

    /// <summary>
    /// 周日记为 7
    /// </summary>
    [Fact]
    public async Task Environment_Weekday_SundayIsSeven()
    {
        var sunday = new DateTimeOffset(2026, 10, 25, 4, 0, 0, TimeSpan.Zero);
        var source = CreateSource(new SaasConditionContext { TenantId = TenantId, Now = sunday });

        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentWeekday, 7);
    }

    /// <summary>
    /// IP 优先取上下文显式给的地址，地区按离线库解析；旧写法的归属地是国家省份城市拼起来
    /// </summary>
    [Fact]
    public async Task Environment_IpAndRegion()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.8");
        var source = CreateSource(new SaasConditionContext
        {
            TenantId = TenantId,
            Now = Now,
            HttpContext = httpContext,
            RemoteIpAddress = IPAddress.Parse("114.114.114.114")
        });

        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentIp, IPAddress.Parse("114.114.114.114"));
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentGeoCountry, "中国");
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentGeoProvince, "江苏省");
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentGeoCity, "南京市");
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentGeoIsp, "电信");
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentLocation, "中国江苏省南京市");
        Assert.Equal(1, _ipRegions.Calls);
    }

    /// <summary>
    /// 离线库查不到的地址（内网）：地区属性取不到；没给显式地址时取连接上的地址
    /// </summary>
    [Fact]
    public async Task Environment_UnknownRegion_IsMissing()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.8");
        var source = CreateSource(new SaasConditionContext { TenantId = TenantId, Now = Now, HttpContext = httpContext });

        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentIp, IPAddress.Parse("10.0.0.8"));
        await AssertMissingAsync(source, SaasConditionAttributes.EnvironmentGeoCountry);
        await AssertMissingAsync(source, SaasConditionAttributes.EnvironmentLocation);
    }

    /// <summary>
    /// 客户端类型：开放接口签名客户端、带 client_id 的 OAuth 令牌、带 session_id 的网页令牌、匿名
    /// </summary>
    [Fact]
    public async Task Environment_ClientKind()
    {
        var openApi = new DefaultHttpContext();
        openApi.Items[OpenApiSecurityConstants.SecurityClientContextKey] = new OpenApiSecurityClient { AccessKey = "ak-1" };
        // 开放接口优先于令牌
        openApi.User = Principal((XiHanClaimTypes.SessionId, "s-1"));
        var oauth = new DefaultHttpContext { User = Principal((XiHanClaimTypes.ClientId, "app-1"), (XiHanClaimTypes.SessionId, "placeholder")) };
        var web = new DefaultHttpContext { User = Principal((XiHanClaimTypes.SessionId, "s-1")) };
        var anonymous = new DefaultHttpContext();

        await AssertClientAsync(openApi, SaasConditionClientKinds.OpenApi, "ak-1");
        await AssertClientAsync(oauth, SaasConditionClientKinds.OAuth, "app-1");
        await AssertClientAsync(web, SaasConditionClientKinds.Web, null);
        await AssertClientAsync(anonymous, SaasConditionClientKinds.Anonymous, null);
    }

    /// <summary>
    /// 请求类资源属性：路径、方法、端点上的服务与方法名、权限码，路由值与查询串（同名多值取第一个）
    /// </summary>
    [Fact]
    public async Task Resource_RequestAttributes()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = "post";
        httpContext.Request.Path = "/api/User/Export";
        httpContext.Request.QueryString = new QueryString("?status=draft&status=archived");
        httpContext.Request.RouteValues["id"] = 42;
        httpContext.Request.Headers.UserAgent = "Mozilla/5.0";
        httpContext.SetEndpoint(new Endpoint(
            null,
            new EndpointMetadataCollection(
                new PermissionAuthorizeAttribute("saas:user:export"),
                new PermissionAuthorizeAttribute("saas:user:read"),
                new ControllerActionDescriptor { ControllerName = "User", ActionName = "Export" }),
            "test"));
        var source = CreateSource(new SaasConditionContext { TenantId = TenantId, Now = Now, HttpContext = httpContext });

        await AssertValuesAsync(source, SaasConditionAttributes.ResourcePath, "/api/User/Export");
        await AssertValuesAsync(source, SaasConditionAttributes.ResourceMethod, "POST");
        await AssertValuesAsync(source, SaasConditionAttributes.ResourceService, "User");
        await AssertValuesAsync(source, SaasConditionAttributes.ResourceAction, "Export");
        await AssertValuesAsync(source, SaasConditionAttributes.ResourcePermissionCodes, "saas:user:export", "saas:user:read");
        await AssertValuesAsync(source, "resource.route.ID", "42");
        await AssertValuesAsync(source, "resource.query.Status", "draft");
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentClientUserAgent, "Mozilla/5.0");
        await AssertMissingAsync(source, "resource.query.absent");
    }

    /// <summary>
    /// 上下文显式给了权限码时用它（试算、登录检查），不看端点
    /// </summary>
    [Fact]
    public async Task Resource_PermissionCodes_ExplicitWins()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new PermissionAuthorizeAttribute("saas:user:read")), "test"));
        var source = CreateSource(new SaasConditionContext
        {
            TenantId = TenantId,
            Now = Now,
            HttpContext = httpContext,
            PermissionCodes = ["saas:role:export"]
        });

        await AssertValuesAsync(source, SaasConditionAttributes.ResourcePermissionCodes, "saas:role:export");
    }

    /// <summary>
    /// 没有请求时，请求类属性与客户端属性都取不到
    /// </summary>
    [Fact]
    public async Task WithoutHttpContext_RequestAttributesAreMissing()
    {
        var source = CreateSource(new SaasConditionContext { TenantId = TenantId, Now = Now });

        foreach (var name in new[]
        {
            SaasConditionAttributes.ResourcePath,
            SaasConditionAttributes.ResourceMethod,
            SaasConditionAttributes.ResourceService,
            SaasConditionAttributes.ResourcePermissionCodes,
            SaasConditionAttributes.EnvironmentIp,
            SaasConditionAttributes.EnvironmentClientKind,
            SaasConditionAttributes.EnvironmentClientUserAgent,
            "resource.route.id"
        })
        {
            await AssertMissingAsync(source, name);
        }
    }

    /// <summary>
    /// 主体属性：角色取授权快照，任职只算启用的部门与其上的岗位，成员类型与租户编码按指定租户读
    /// </summary>
    [Fact]
    public async Task Subject_Attributes()
    {
        var source = CreateSource(new SaasConditionContext
        {
            TenantId = TenantId,
            Now = Now,
            UserId = UserId,
            UserName = "alice",
            IsImpersonating = true
        });

        await AssertValuesAsync(source, SaasConditionAttributes.SubjectUserId, "1001");
        await AssertValuesAsync(source, SaasConditionAttributes.SubjectUserName, "alice");
        await AssertValuesAsync(source, SaasConditionAttributes.SubjectIsImpersonating, true);
        await AssertValuesAsync(source, SaasConditionAttributes.SubjectRoleCodes, "clerk", "auditor");
        await AssertValuesAsync(source, SaasConditionAttributes.SubjectDepartmentCodes, "sales", "finance");
        await AssertValuesAsync(source, SaasConditionAttributes.SubjectMainDepartmentCode, "sales");
        await AssertValuesAsync(source, SaasConditionAttributes.SubjectPositionCodes, "manager", "clerk");
        await AssertValuesAsync(source, SaasConditionAttributes.SubjectMemberType, "External");
        await AssertValuesAsync(source, SaasConditionAttributes.SubjectTenantCode, "acme");

        // 主体与当前上下文不同（登录检查、试算）时，按上下文里的租户读，读完还原
        Assert.Equal([TenantId], _tenantSeenBySnapshot);
        Assert.Null(_currentTenant.Id);
        // 部门、主部门、岗位共用一次任职读取
        _userDepartments.Verify(repository => repository.GetValidByUserIdAsync(UserId, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 平台上下文没有租户编码与成员类型；未登录时主体属性都取不到，也不查库
    /// </summary>
    [Fact]
    public async Task Subject_PlatformAndAnonymous()
    {
        var platform = CreateSource(new SaasConditionContext { TenantId = 0, Now = Now, UserId = UserId });
        await AssertMissingAsync(platform, SaasConditionAttributes.SubjectTenantCode);
        await AssertMissingAsync(platform, SaasConditionAttributes.SubjectMemberType);

        var anonymous = CreateSource(new SaasConditionContext { TenantId = TenantId, Now = Now });
        foreach (var name in new[]
        {
            SaasConditionAttributes.SubjectUserId,
            SaasConditionAttributes.SubjectUserName,
            SaasConditionAttributes.SubjectIsImpersonating,
            SaasConditionAttributes.SubjectRoleCodes,
            SaasConditionAttributes.SubjectDepartmentCodes,
            SaasConditionAttributes.SubjectMemberType
        })
        {
            await AssertMissingAsync(anonymous, name);
        }

        _snapshots.VerifyNoOtherCalls();
        _userDepartments.VerifyNoOtherCalls();
        _tenantUsers.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 惰性取值：只引用环境属性时不读任何主体数据
    /// </summary>
    [Fact]
    public async Task Source_IsLazy()
    {
        var source = CreateSource(new SaasConditionContext { TenantId = TenantId, Now = Now, UserId = UserId });

        _ = await source.GetAsync(SaasConditionAttributes.EnvironmentHour);
        _ = await source.GetAsync(SaasConditionAttributes.SubjectUserId);

        _snapshots.VerifyNoOtherCalls();
        _userDepartments.VerifyNoOtherCalls();
        _departments.VerifyNoOtherCalls();
        _tenantUsers.VerifyNoOtherCalls();
        _tenants.VerifyNoOtherCalls();
    }

    /// <summary>
    /// 接上框架的编译器与求值器：旧名、IP 网段、跨零点时段、权限码后缀
    /// </summary>
    [Fact]
    public async Task EndToEnd_WithFrameworkEvaluator()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("::ffff:10.1.2.3");
        httpContext.SetEndpoint(new Endpoint(null, new EndpointMetadataCollection(new PermissionAuthorizeAttribute("saas:user:export")), "test"));
        var source = CreateSource(new SaasConditionContext { TenantId = TenantId, Now = Now, UserId = UserId, HttpContext = httpContext });
        var registry = new DefaultConditionAttributeRegistry(SaasConditionAttributes.All);
        var compiled = new DefaultConditionCompiler(registry).Compile(
            new ConditionSet(
            [
                new ConditionGroup(
                [
                    new ConditionClause("subject.role", FrameworkConditionOperator.In, "auditor,admin"),
                    new ConditionClause(SaasConditionAttributes.EnvironmentIp, FrameworkConditionOperator.In, "10.0.0.0/8"),
                    new ConditionClause(SaasConditionAttributes.EnvironmentTime, FrameworkConditionOperator.Between, "22:00,06:00"),
                    new ConditionClause("resource.permissionCode", FrameworkConditionOperator.EndsWith, ":export"),
                    new ConditionClause(SaasConditionAttributes.SubjectDepartmentCodes, FrameworkConditionOperator.Contains, "finance")
                ])
            ]),
            new ConditionCompileOptions { TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Shanghai") });

        Assert.Empty(compiled.Errors);
        var result = await new DefaultConditionEvaluator().EvaluateAsync(compiled, source, explain: true);

        Assert.Equal(ConditionOutcome.Matched, result.Outcome);
    }

    /// <summary>
    /// 注册：登记项进登记表，缺省时区 Asia/Shanghai；时区配错在启动校验时就报
    /// </summary>
    [Fact]
    public void Registration_ValidatesTimeZone()
    {
        Assert.Equal("Asia/Shanghai", new SaasConditionOptions().TimeZone);
        Assert.Equal(new SaasConditionOptions().ResolveTimeZone().BaseUtcOffset, TimeSpan.FromHours(8));

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Saas:Conditions:TimeZone"] = "Mars/Olympus" })
            .Build());
        services.AddSaasConditions();
        using var provider = services.BuildServiceProvider();

        var registered = provider.GetRequiredService<IOptions<XiHanConditionAttributeOptions>>().Value.Attributes;
        Assert.Contains(registered, descriptor => descriptor.Name == SaasConditionAttributes.EnvironmentIp);
        var error = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<SaasConditionOptions>>().Value);
        Assert.Contains("Mars/Olympus", error.Message);
    }

    private SaasConditionAttributeSource CreateSource(SaasConditionContext context)
    {
        var loader = new SaasConditionSubjectLoader(
            _snapshots.Object,
            _userDepartments.Object,
            _departments.Object,
            _positions.Object,
            _tenantUsers.Object,
            _tenants.Object,
            _currentTenant);
        return new SaasConditionAttributeSource(context, loader, _ipRegions, new SaasConditionOptions().ResolveTimeZone());
    }

    private async Task AssertClientAsync(HttpContext httpContext, string kind, string? appId)
    {
        var source = CreateSource(new SaasConditionContext { TenantId = TenantId, Now = Now, HttpContext = httpContext });
        await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentClientKind, kind);
        if (appId is null)
        {
            await AssertMissingAsync(source, SaasConditionAttributes.EnvironmentClientAppId);
        }
        else
        {
            await AssertValuesAsync(source, SaasConditionAttributes.EnvironmentClientAppId, appId);
        }
    }

    private static async Task AssertValuesAsync(IConditionAttributeSource source, string name, params object[] expected)
    {
        var value = await source.GetAsync(name);
        Assert.Equal(ConditionAttributeValueState.Present, value.State);
        Assert.Equal(expected, value.Values);
    }

    private static async Task AssertMissingAsync(IConditionAttributeSource source, string name)
    {
        var value = await source.GetAsync(name);
        Assert.Equal(ConditionAttributeValueState.Missing, value.State);
    }

    private static ClaimsPrincipal Principal(params (string Type, string Value)[] claims)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(claims.Select(claim => new Claim(claim.Type, claim.Value)), "test"));
    }

    private static SysDepartment Department(long id, string code, EnableStatus status = EnableStatus.Enabled)
    {
        var department = new SysDepartment { DepartmentCode = code, DepartmentName = code, Status = status };
        SaasTestHelper.SetBasicId(department, id);
        return department;
    }

    private static SysPosition Position(long id, string code)
    {
        var position = new SysPosition { PositionCode = code, PositionName = code };
        SaasTestHelper.SetBasicId(position, id);
        return position;
    }

    private sealed class FakeIpRegionResolver : IIpRegionResolver
    {
        public Dictionary<IPAddress, IpRegion> Regions { get; } = [];

        public int Calls { get; private set; }

        public IpRegion? Resolve(IPAddress address)
        {
            Calls++;
            return Regions.GetValueOrDefault(address);
        }
    }
}
