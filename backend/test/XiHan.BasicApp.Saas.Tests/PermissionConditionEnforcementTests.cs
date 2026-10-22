// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Core.Clients;
using FrameworkConditionOperator = XiHan.Framework.Authorization.Conditions.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限条件参与鉴权：<c>[PermissionAuthorize]</c> 走带请求上下文的判定，模式开关、后台调用与数据行属性补算
/// </summary>
public sealed class PermissionConditionEnforcementTests
{
    private const long UserId = 1001;
    private const string Code = "saas:position:update";
    private const string OtherCode = "saas:position:read";

    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IPermissionConditionHitRecorder> _hitRecorder = new();
    private readonly PermissionConditionTestKit.FixedOptionsMonitor<SaasPermissionConditionOptions> _options = new(new SaasPermissionConditionOptions());
    private readonly HttpContextAccessor _httpContextAccessor = new();
    private readonly IPermissionResourceRegistry _resources = PermissionConditionTestKit.Resources(
        PermissionResourceRegistration.Create<SysPosition>(Code, PermissionResourceKeys.FromArgument("id")));

    private AuthorizationSnapshot _snapshot = new(["clerk"], [Code, OtherCode], [], new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// 构造测试上下文
    /// </summary>
    public PermissionConditionEnforcementTests()
    {
        _currentUser.Setup(user => user.UserId).Returns(UserId);
        _currentUser.Setup(user => user.UserName).Returns("alice");
    }

    /// <summary>
    /// 只有有条件授予：条件满足放行；不满足返回「当前条件下不允许此操作」；快照没授予仍是「权限不足」
    /// </summary>
    [Fact]
    public async Task CheckAsync_ConditionalGrant()
    {
        GrantWhen(SaasConditionAttributes.EnvironmentIp, FrameworkConditionOperator.In, "10.0.0.0/8");
        var checker = CreateChecker();

        Assert.True((await checker.CheckAsync(Context(Code, Request("10.1.2.3")))).IsGranted);

        var denied = await checker.CheckAsync(Context(Code, Request("8.8.8.8")));
        Assert.False(denied.IsGranted);
        Assert.Equal(PermissionFailureCodes.ConditionNotMet, denied.FailureCode);

        var notGranted = await checker.CheckAsync(Context("saas:tenant:create", Request("10.1.2.3")));
        Assert.Equal(PermissionFailureCodes.NotGranted, notGranted.FailureCode);
        _hitRecorder.Verify(recorder => recorder.RecordAsync(It.Is<PermissionConditionHit>(hit => hit.PermissionCode == Code && hit.Mode == PermissionConditionMode.Enforce), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 只记录：不满足照常放行，记一次；应急不求值：有条件授予按授予处理；改配置热生效
    /// </summary>
    [Fact]
    public async Task Modes_LogOnlyAndDisabled()
    {
        GrantWhen(SaasConditionAttributes.EnvironmentIp, FrameworkConditionOperator.In, "10.0.0.0/8");

        _options.CurrentValue = new SaasPermissionConditionOptions { Mode = PermissionConditionMode.LogOnly };
        Assert.True((await CreateChecker().CheckAsync(Context(Code, Request("8.8.8.8")))).IsGranted);
        _hitRecorder.Verify(recorder => recorder.RecordAsync(It.Is<PermissionConditionHit>(hit => hit.Mode == PermissionConditionMode.LogOnly), It.IsAny<CancellationToken>()), Times.Once);

        _options.CurrentValue = new SaasPermissionConditionOptions { Mode = PermissionConditionMode.Disabled };
        Assert.True((await CreateChecker().CheckAsync(Context(Code, Request("8.8.8.8")))).IsGranted);
        _hitRecorder.VerifyNoOtherCalls();

        _options.CurrentValue = new SaasPermissionConditionOptions { Mode = PermissionConditionMode.Enforce };
        Assert.False((await CreateChecker().CheckAsync(Context(Code, Request("8.8.8.8")))).IsGranted);
    }

    /// <summary>
    /// 不带上下文的检查取当前请求；没有请求（后台续做）时不求条件，按有条件授予放行
    /// </summary>
    [Fact]
    public async Task IsGrantedAsync_UsesAmbientRequest_BackgroundSkipsConditions()
    {
        GrantWhen(SaasConditionAttributes.EnvironmentIp, FrameworkConditionOperator.In, "10.0.0.0/8");
        var checker = CreateChecker();

        _httpContextAccessor.HttpContext = Request("8.8.8.8");
        Assert.False(await checker.IsGrantedAsync(UserId.ToString(), Code));
        Assert.False(await checker.IsAllGrantedAsync(UserId.ToString(), [Code, OtherCode]));
        Assert.True(await checker.IsAnyGrantedAsync(UserId.ToString(), [Code, OtherCode]));

        _httpContextAccessor.HttpContext = null;
        Assert.True(await CreateChecker().IsGrantedAsync(UserId.ToString(), Code));
        Assert.True((await CreateChecker().CheckAsync(new PermissionCheckContext(UserId.ToString(), Code))).IsGranted);
    }

    /// <summary>
    /// 查的不是当前用户本人：请求属于别人，不拿它求条件
    /// </summary>
    [Fact]
    public async Task OtherUser_ConditionsNotEvaluatedAgainstCallerRequest()
    {
        GrantWhen(SaasConditionAttributes.EnvironmentIp, FrameworkConditionOperator.In, "10.0.0.0/8");

        Assert.True((await CreateChecker().CheckAsync(new PermissionCheckContext("2002", Code, resource: Request("8.8.8.8")))).IsGranted);
    }

    /// <summary>
    /// 有条件的授权在「我的权限」里照常显示
    /// </summary>
    [Fact]
    public async Task GrantedPermissions_ListConditionalGrants()
    {
        GrantWhen(SaasConditionAttributes.EnvironmentIp, FrameworkConditionOperator.In, "10.0.0.0/8");
        _httpContextAccessor.HttpContext = Request("8.8.8.8");

        Assert.Contains(Code, await CreateChecker().GetGrantedPermissionsAsync(UserId.ToString()));
    }

    /// <summary>
    /// 数据行属性：鉴权时待定，MVC 动作上先放行并记下；不是 MVC 动作的端点等不到补算，按不满足处理
    /// </summary>
    [Fact]
    public async Task RowAttributes_PendingUntilModelBinding()
    {
        GrantWhen("resource.status", FrameworkConditionOperator.Equals, nameof(EnableStatus.Enabled));

        var mvc = Request("10.1.2.3", mvc: true);
        Assert.True((await CreateChecker().CheckAsync(Context(Code, mvc))).IsGranted);
        Assert.Single(PendingPermissionConditions.Take(mvc));

        var minimal = Request("10.1.2.3", mvc: false);
        Assert.Equal(PermissionFailureCodes.ConditionNotMet, (await CreateChecker().CheckAsync(Context(Code, minimal))).FailureCode);
    }

    /// <summary>
    /// 模型绑定之后按主键读行补算：行满足进入动作；不满足返回 403 不进动作；读不到行也不满足
    /// </summary>
    [Theory]
    [InlineData(EnableStatus.Enabled, true)]
    [InlineData(EnableStatus.Disabled, false)]
    public async Task RowFilter_EvaluatesLoadedRow(EnableStatus status, bool expected)
    {
        GrantWhen("resource.status", FrameworkConditionOperator.Equals, nameof(EnableStatus.Enabled));
        var httpContext = Request("10.1.2.3", mvc: true);
        await CreateChecker().CheckAsync(Context(Code, httpContext));
        var reader = new FakeRowReader(Position(7, status));

        var (nextCalled, context) = await RunFilterAsync(httpContext, reader, new Dictionary<string, object?> { ["id"] = 7L });

        Assert.Equal(expected, nextCalled);
        Assert.Equal(expected ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden, context.HttpContext.Response.StatusCode);
        Assert.Equal([7L], reader.RequestedIds);
    }

    /// <summary>
    /// 批量主键逐行算，每行都要满足；取不到主键时行属性取不到，不满足
    /// </summary>
    [Fact]
    public async Task RowFilter_BatchAndMissingKeys()
    {
        GrantWhen("resource.status", FrameworkConditionOperator.Equals, nameof(EnableStatus.Enabled));
        var reader = new FakeRowReader(Position(7, EnableStatus.Enabled), Position(8, EnableStatus.Disabled));

        var batch = Request("10.1.2.3", mvc: true);
        await CreateChecker().CheckAsync(Context(Code, batch));
        Assert.False((await RunFilterAsync(batch, reader, new Dictionary<string, object?> { ["id"] = new List<long> { 7, 8 } })).NextCalled);

        var noKey = Request("10.1.2.3", mvc: true);
        await CreateChecker().CheckAsync(Context(Code, noKey));
        Assert.False((await RunFilterAsync(noKey, reader, new Dictionary<string, object?>())).NextCalled);
    }

    /// <summary>
    /// 进了动作之后再查权限（如按操作校验的守卫）：补算已经发生过，等不到下一次，含行属性的条件按不满足处理
    /// </summary>
    [Fact]
    public async Task RowAttributes_CheckedInsideAction_DoNotDefer()
    {
        GrantWhen("resource.status", FrameworkConditionOperator.Equals, nameof(EnableStatus.Enabled));
        var httpContext = Request("10.1.2.3", mvc: true);
        _ = await RunFilterAsync(httpContext, new FakeRowReader(), []);
        _httpContextAccessor.HttpContext = httpContext;

        Assert.False(await CreateChecker().IsGrantedAsync(UserId.ToString(), Code));
        Assert.Empty(PendingPermissionConditions.Take(httpContext));
    }

    /// <summary>
    /// 只记录模式下补算不满足也进入动作，只记一次
    /// </summary>
    [Fact]
    public async Task RowFilter_LogOnlyRecordsButContinues()
    {
        _options.CurrentValue = new SaasPermissionConditionOptions { Mode = PermissionConditionMode.LogOnly };
        GrantWhen("resource.status", FrameworkConditionOperator.Equals, nameof(EnableStatus.Enabled));
        var httpContext = Request("10.1.2.3", mvc: true);
        await CreateChecker().CheckAsync(Context(Code, httpContext));

        var (nextCalled, _) = await RunFilterAsync(httpContext, new FakeRowReader(Position(7, EnableStatus.Disabled)), new Dictionary<string, object?> { ["id"] = 7L });

        Assert.True(nextCalled);
        _hitRecorder.Verify(recorder => recorder.RecordAsync(It.IsAny<PermissionConditionHit>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 登记：公开标量属性按 camelCase 映射；标了排除的、与请求类属性同名的不映射
    /// </summary>
    [Fact]
    public void Registration_MapsScalarProperties()
    {
        var registration = PermissionResourceRegistration.Create<GuardedPosition>("saas:position:audit", PermissionResourceKeys.FromArgument("id"));
        var names = registration.Attributes.Select(attribute => attribute.Name).ToList();

        Assert.Contains("resource.positionCode", names);
        Assert.Contains("resource.status", names);
        Assert.DoesNotContain("resource.secret", names);
        Assert.DoesNotContain("resource.path", names);
        Assert.Throws<InvalidOperationException>(() => PermissionConditionTestKit.Resources(registration, registration));
    }

    private void GrantWhen(string attribute, FrameworkConditionOperator @operator, string value)
    {
        var conditions = new AuthorizationConditions();
        conditions.Grants[Code] = [new ConditionalGrantPath { Source = "role:1", GrantSetKeys = ["rp:1"] }];
        conditions.Sets["rp:1"] = new AuthorizationConditionSet
        {
            Key = "rp:1",
            PermissionCode = Code,
            Fingerprint = $"{attribute}|{value}",
            Clauses = [new AuthorizationConditionClause { Id = 1, AttributeName = attribute, Operator = (int)@operator, Value = value }]
        };
        _snapshot = _snapshot with { Conditions = conditions };
    }

    private SaasPermissionChecker CreateChecker()
    {
        var snapshots = new Mock<IAuthorizationSnapshotQueryService>();
        snapshots
            .Setup(service => service.BuildAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => _snapshot);
        var sessions = new Mock<IUserSessionRepository>();
        var currentTenant = new TestCurrentTenant(7);
        var loader = new SaasConditionSubjectLoader(
            snapshots.Object,
            new Mock<IUserDepartmentRepository>().Object,
            new Mock<IDepartmentRepository>().Object,
            new Mock<IPositionRepository>().Object,
            new Mock<ITenantUserRepository>().Object,
            new Mock<ITenantRepository>().Object,
            currentTenant);
        var sources = new SaasConditionAttributeSourceFactory(
            _httpContextAccessor,
            _currentUser.Object,
            currentTenant,
            loader,
            new Mock<IIpRegionResolver>().Object,
            Options.Create(new SaasConditionOptions()));
        var enforcer = new PermissionConditionEnforcer(
            PermissionConditionTestKit.Decider(_resources),
            sources,
            _resources,
            _options,
            _hitRecorder.Object,
            _currentUser.Object,
            currentTenant);
        return new SaasPermissionChecker(snapshots.Object, sessions.Object, _currentUser.Object, _httpContextAccessor, enforcer);
    }

    private async Task<(bool NextCalled, ActionExecutingContext Context)> RunFilterAsync(HttpContext httpContext, FakeRowReader reader, Dictionary<string, object?> arguments)
    {
        var filter = new PermissionConditionRowFilter(_resources, PermissionConditionTestKit.Decider(_resources), reader, _hitRecorder.Object);
        var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
        var context = new ActionExecutingContext(actionContext, [], arguments, controller: new object());
        var nextCalled = false;
        await filter.OnActionExecutionAsync(context, () =>
        {
            nextCalled = true;
            return Task.FromResult(new ActionExecutedContext(actionContext, [], controller: new object()));
        });
        return (nextCalled, context);
    }

    private static PermissionCheckContext Context(string code, HttpContext httpContext)
    {
        return new PermissionCheckContext(UserId.ToString(), code, httpContext.User, httpContext);
    }

    private static DefaultHttpContext Request(string ip, bool mvc = true)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, UserId.ToString())], "test"))
        };
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        httpContext.Response.Body = new MemoryStream();
        httpContext.SetEndpoint(mvc
            ? new Endpoint(null, new EndpointMetadataCollection(new ControllerActionDescriptor()), "mvc")
            : new Endpoint(null, new EndpointMetadataCollection(), "minimal"));
        return httpContext;
    }

    private static SysPosition Position(long id, EnableStatus status)
    {
        var position = new SysPosition { PositionCode = $"p{id}", PositionName = $"p{id}", Status = status };
        SaasTestHelper.SetBasicId(position, id);
        return position;
    }

    /// <summary>
    /// 带排除列与保留名列的测试实体
    /// </summary>
    public sealed class GuardedPosition : SysPosition
    {
        /// <summary>
        /// 不参与条件的列
        /// </summary>
        [ExcludeFromConditions]
        public string Secret { get; set; } = string.Empty;

        /// <summary>
        /// 与请求类属性 resource.path 同名
        /// </summary>
        public string Path { get; set; } = string.Empty;
    }

    private sealed class FakeRowReader(params object[] rows) : IPermissionResourceRowReader
    {
        public List<long> RequestedIds { get; } = [];

        public Task<IReadOnlyList<object>> ReadAsync(PermissionResourceRegistration registration, IReadOnlyCollection<long> ids, CancellationToken cancellationToken = default)
        {
            RequestedIds.AddRange(ids);
            return Task.FromResult<IReadOnlyList<object>>([.. rows.Where(row => ids.Contains(PermissionResourceRegistration.KeyOf(row)))]);
        }
    }
}
