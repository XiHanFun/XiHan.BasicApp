// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.Conditions;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.Web.Core.Clients;
using ConditionOperator = XiHan.BasicApp.Saas.Domain.Entities.ConditionOperator;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限条件的盘点、试算与属性目录
/// </summary>
public sealed class PermissionConditionInsightTests
{
    private const long TenantId = 7;
    private const long UserId = 1001;
    private const long RoleId = 11;
    private const long ChildRoleId = 12;
    private const long PermissionId = 5;
    private const long RolePermissionId = 501;
    private const string Code = "saas:position:update";

    private static readonly DateTimeOffset Now = new(2026, 10, 23, 2, 0, 0, TimeSpan.Zero);

    private readonly TestCurrentTenant _currentTenant = new();
    private readonly Mock<IPermissionConditionRepository> _conditions = new();
    private readonly Mock<IRolePermissionRepository> _rolePermissions = new();
    private readonly Mock<IUserPermissionRepository> _userPermissions = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IPermissionRepository> _permissions = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ITenantRepository> _tenants = new();
    private readonly Mock<IAuthorizationSnapshotQueryService> _snapshots = new();
    private readonly Mock<IUserRoleRepository> _userRoles = new();
    private readonly Mock<IRoleHierarchyDomainService> _hierarchy = new();
    private readonly List<long?> _snapshotTenants = [];
    private readonly IPermissionResourceRegistry _resources = PermissionConditionTestKit.Resources(
        PermissionResourceRegistration.Create<SysPosition>(Code, PermissionResourceKeys.FromArgument("id")));

    private AuthorizationSnapshot _snapshot = new([], [Code], [], new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    /// <summary>
    /// 构造测试上下文
    /// </summary>
    public PermissionConditionInsightTests()
    {
        var tenant = new SysTenant { TenantCode = "acme" };
        SaasTestHelper.SetBasicId(tenant, TenantId);
        _tenants.Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>())).ReturnsAsync([tenant]);
        var role = new SysRole { RoleCode = "clerk", RoleName = "办事员", Status = EnableStatus.Enabled };
        SaasTestHelper.SetBasicId(role, RoleId);
        _roles.Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>())).ReturnsAsync([role]);
        var permission = new SysPermission { PermissionCode = Code, PermissionName = "岗位更新", Status = EnableStatus.Enabled };
        SaasTestHelper.SetBasicId(permission, PermissionId);
        _permissions.Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>())).ReturnsAsync([permission]);
        _permissions.Setup(repository => repository.GetByIdAsync(PermissionId, It.IsAny<CancellationToken>())).ReturnsAsync(permission);
        var rolePermission = new SysRolePermission { RoleId = RoleId, PermissionId = PermissionId, PermissionAction = PermissionAction.Grant, Status = ValidityStatus.Valid };
        SaasTestHelper.SetBasicId(rolePermission, RolePermissionId);
        _rolePermissions.Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>())).ReturnsAsync([rolePermission]);
        _rolePermissions.Setup(repository => repository.GetByIdAsync(RolePermissionId, It.IsAny<CancellationToken>())).ReturnsAsync(rolePermission);
        _userPermissions.Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
        var user = new SysUser { UserName = "alice" };
        SaasTestHelper.SetBasicId(user, UserId);
        _users.Setup(repository => repository.GetListByIdsIgnoreTenantAsync(It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>())).ReturnsAsync([user]);
        _users.Setup(repository => repository.GetByIdIgnoreTenantAsync(UserId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _snapshots
            .Setup(service => service.BuildAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                _snapshotTenants.Add(_currentTenant.Id);
                return _snapshot;
            });
    }

    /// <summary>
    /// 盘点：逐条诊断，按组给结论；平台上下文跨租户读，并切到条件所在租户读授权
    /// </summary>
    [Fact]
    public async Task Inventory_DiagnosesEachConditionAndConcludesGroups()
    {
        _conditions.Setup(repository => repository.GetForInventoryAsync(true, It.IsAny<CancellationToken>())).ReturnsAsync(
        [
            Condition(1, group: 0, "environment.ip", ConditionOperator.In, "10.0.0.0/8"),
            Condition(2, group: 0, "subject.role", ConditionOperator.In, "clerk"),
            Condition(3, group: 1, "environment.ip", ConditionOperator.StartsWith, "192.168."),
            Condition(4, group: 2, "resource.amount", ConditionOperator.LessThan, "10000"),
            Condition(5, group: 3, "environment.hour", ConditionOperator.Between, "9,18", ConfigDataType.Boolean),
            Condition(6, group: 4, "environment.hour", ConditionOperator.Between, "9,18", status: ValidityStatus.Invalid),
            new SysPermissionCondition { RolePermissionId = RolePermissionId, UserPermissionId = 9, AttributeName = "environment.hour", ConditionValue = "1", Status = ValidityStatus.Valid }
        ]);

        var inventory = await CreateInventory().BuildAsync(Now);

        Assert.Equal(7, inventory.ConditionCount);
        var byId = inventory.Items.Where(item => item.ConditionId > 0).ToDictionary(item => item.ConditionId);
        Assert.Equal("acme", byId[1].TenantCode);
        Assert.Equal("clerk", byId[1].RoleCode);
        Assert.Equal(Code, byId[1].PermissionCode);
        Assert.Equal(PermissionAction.Grant, byId[1].PermissionAction);
        Assert.Empty(byId[1].Diagnostics);
        Assert.Equal([PermissionConditionDiagnostic.LegacyName], byId[2].Diagnostics);
        Assert.Equal(PermissionConditionGroupConclusion.Evaluable, byId[1].GroupConclusion);
        Assert.Equal([PermissionConditionDiagnostic.InvalidCondition], byId[3].Diagnostics);
        Assert.Equal(PermissionConditionGroupConclusion.NeverMatches, byId[3].GroupConclusion);
        Assert.Equal([PermissionConditionDiagnostic.UnregisteredAttribute], byId[4].Diagnostics);
        Assert.Equal("数据行属性未按权限码登记", byId[4].Message);
        Assert.Equal([PermissionConditionDiagnostic.ValueTypeMismatch], byId[5].Diagnostics);
        Assert.Equal(PermissionConditionGroupConclusion.Evaluable, byId[5].GroupConclusion);
        Assert.Equal(PermissionConditionGroupConclusion.Disabled, byId[6].GroupConclusion);
        var ambiguous = Assert.Single(inventory.Items, item => item.BindingKind == PermissionConditionBindingKind.Ambiguous);
        Assert.Equal(PermissionConditionGroupConclusion.BindingInactive, ambiguous.GroupConclusion);
        Assert.Equal(2, inventory.GroupConclusions[PermissionConditionGroupConclusion.NeverMatches]);
        Assert.Equal(1, inventory.Diagnostics[PermissionConditionDiagnostic.LegacyName]);
        Assert.Null(_currentTenant.Id);
    }

    /// <summary>
    /// 盘点：角色停用后其授权上的条件不影响任何人；租户上下文只读本租户
    /// </summary>
    [Fact]
    public async Task Inventory_InactiveBindingAndTenantScope()
    {
        _currentTenant.Change(TenantId);
        _conditions.Setup(repository => repository.GetForInventoryAsync(false, It.IsAny<CancellationToken>())).ReturnsAsync([Condition(1, 0, "environment.ip", ConditionOperator.In, "10.0.0.0/8")]);
        var disabledRole = new SysRole { RoleCode = "clerk", Status = EnableStatus.Disabled };
        SaasTestHelper.SetBasicId(disabledRole, RoleId);
        _roles.Setup(repository => repository.GetByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>())).ReturnsAsync([disabledRole]);

        var item = Assert.Single((await CreateInventory().BuildAsync(Now)).Items);

        Assert.Equal([PermissionConditionDiagnostic.BindingInactive], item.Diagnostics);
        Assert.Equal(PermissionConditionGroupConclusion.BindingInactive, item.GroupConclusion);
        _conditions.Verify(repository => repository.GetForInventoryAsync(true, It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 盘点汇总的各类条数：写成「名称 条数」逗号连接，一条都没有时写「无」
    /// </summary>
    [Fact]
    public void Inventory_FormatCounts()
    {
        Assert.Equal("无", PermissionConditionInventoryService.FormatCounts(new Dictionary<PermissionConditionDiagnostic, int>()));
        Assert.Equal(
            "Evaluable 2，NeverMatches 1",
            PermissionConditionInventoryService.FormatCounts(new Dictionary<PermissionConditionGroupConclusion, int>
            {
                [PermissionConditionGroupConclusion.Evaluable] = 2,
                [PermissionConditionGroupConclusion.NeverMatches] = 1
            }));
    }

    /// <summary>
    /// 单人试算：在目标租户里取快照，给出每条路径、每组每个子句的取值与结果
    /// </summary>
    [Fact]
    public async Task Trial_ExplainsVerdictPath()
    {
        UseConditionalGrant(("environment.ip", (int)FrameworkOperator.In, "10.0.0.0/8", false), ("environment.hour", (int)FrameworkOperator.Between, "9,18", false));

        var granted = await CreateTrial().TrialAsync(Input(ip: "10.1.2.3", time: new DateTimeOffset(2026, 10, 23, 10, 0, 0, TimeSpan.FromHours(8))));
        Assert.Equal(PermissionConditionTrialVerdict.Granted, granted.Verdict);
        var path = Assert.Single(granted.Paths);
        Assert.Equal("办事员", path.SourceLabel);
        Assert.Equal(ConditionOutcome.Matched, path.Outcome);
        var clauses = Assert.Single(Assert.Single(path.GrantSets).Groups).Clauses;
        Assert.Equal(["10.1.2.3"], clauses[0].AttributeValues);
        Assert.Equal(["10"], clauses[1].AttributeValues);
        Assert.Equal([(long?)TenantId], _snapshotTenants);
        Assert.Equal("Asia/Shanghai", granted.TimeZone);

        var denied = await CreateTrial().TrialAsync(Input(ip: "8.8.8.8", time: Now));
        Assert.Equal(PermissionConditionTrialVerdict.Denied, denied.Verdict);
        Assert.Equal(ConditionOutcome.NotMatched, Assert.Single(denied.Paths).Outcome);
        // 不满足的组仍给出每个子句的结果（不短路）
        Assert.Equal(2, Assert.Single(Assert.Single(denied.Paths[0].GrantSets).Groups).Clauses.Count);
    }

    /// <summary>
    /// 单人试算：没授予、无条件授予、还差数据行；数据行给了主键就按行算
    /// </summary>
    [Fact]
    public async Task Trial_OtherVerdicts()
    {
        _snapshot = _snapshot with { Permissions = [] };
        Assert.Equal(PermissionConditionTrialVerdict.NotGranted, (await CreateTrial().TrialAsync(Input())).Verdict);

        _snapshot = _snapshot with { Permissions = [Code] };
        Assert.Equal(PermissionConditionTrialVerdict.Unconditional, (await CreateTrial().TrialAsync(Input())).Verdict);

        UseConditionalGrant(("resource.status", (int)FrameworkOperator.Equals, nameof(EnableStatus.Enabled), false));
        Assert.Equal(PermissionConditionTrialVerdict.Pending, (await CreateTrial().TrialAsync(Input())).Verdict);

        var position = new SysPosition { Status = EnableStatus.Disabled };
        SaasTestHelper.SetBasicId(position, 77);
        var input = Input();
        input.ResourceId = 77;
        Assert.Equal(PermissionConditionTrialVerdict.Denied, (await CreateTrial(null, position).TrialAsync(input)).Verdict);
    }

    /// <summary>
    /// 单人试算：IP 不填取当前请求的；写错报出来
    /// </summary>
    [Fact]
    public async Task Trial_DefaultsAndValidation()
    {
        UseConditionalGrant(("environment.ip", (int)FrameworkOperator.In, "10.0.0.0/8", false));
        var current = new DefaultHttpContext();
        current.Connection.RemoteIpAddress = IPAddress.Parse("10.9.9.9");

        var result = await CreateTrial(current).TrialAsync(Input());

        Assert.Equal("10.9.9.9", result.Ip);
        Assert.Equal(PermissionConditionTrialVerdict.Granted, result.Verdict);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateTrial().TrialAsync(Input(ip: "10.0.0.256")));
    }

    /// <summary>
    /// 批量试算：按角色授权找人，含继承该角色的角色成员，只在当前上下文里找
    /// </summary>
    [Fact]
    public async Task BatchTrial_FindsMembersOfRoleAndDescendants()
    {
        _currentTenant.Change(TenantId);
        UseConditionalGrant(("environment.ip", (int)FrameworkOperator.In, "10.0.0.0/8", false));
        _hierarchy.Setup(service => service.GetGraphAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new RoleInheritanceGraph([(RoleId, ChildRoleId)]));
        _userRoles
            .Setup(repository => repository.GetValidByRoleIdsAsync(It.Is<IReadOnlyCollection<long>>(ids => ids.Contains(RoleId) && ids.Contains(ChildRoleId)), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new SysUserRole { UserId = UserId, RoleId = ChildRoleId, TenantId = TenantId },
                new SysUserRole { UserId = 2002, RoleId = RoleId, TenantId = 8 }
            ]);

        var result = await CreateTrial().BatchTrialAsync(new PermissionConditionBatchTrialInputDto { RolePermissionId = RolePermissionId, Ip = "8.8.8.8", Time = Now });

        Assert.Equal(Code, result.PermissionCode);
        Assert.Equal(1, result.TotalUsers);
        var item = Assert.Single(result.Items);
        Assert.Equal(UserId, item.UserId);
        Assert.Equal(PermissionConditionTrialVerdict.Denied, item.Verdict);
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateTrial().BatchTrialAsync(new PermissionConditionBatchTrialInputDto()));
    }

    /// <summary>
    /// 属性目录：给出值种类、能用的运算符、旧名，所绑权限码登记了实体时带上数据行属性
    /// </summary>
    [Fact]
    public async Task Attributes_IncludeOperatorsAndRowAttributes()
    {
        var service = new PermissionConditionInsightQueryService(
            new DefaultConditionAttributeRegistry(SaasConditionAttributes.All),
            _resources,
            CreateInventory(),
            CreateTrial());

        var plain = await service.GetConditionAttributesAsync();
        var withRows = await service.GetConditionAttributesAsync(Code);

        var ip = Assert.Single(plain, item => item.Name == SaasConditionAttributes.EnvironmentIp);
        Assert.Equal(ConditionValueKind.IpAddress, ip.Kind);
        Assert.DoesNotContain(ConditionOperator.StartsWith, ip.Operators);
        Assert.Contains(ConditionOperator.In, ip.Operators);
        Assert.Contains("subject.role", Assert.Single(plain, item => item.Name == SaasConditionAttributes.SubjectRoleCodes).Aliases);
        Assert.True(Assert.Single(plain, item => item.Name == SaasConditionAttributes.EnvironmentLocation).IsLegacy);
        Assert.DoesNotContain(plain, item => item.IsRowAttribute);
        Assert.True(Assert.Single(withRows, item => item.Name == "resource.status").IsRowAttribute);
    }

    private enum FrameworkOperator
    {
        Equals = 0,
        In = 8,
        Between = 10
    }

    private void UseConditionalGrant(params (string Attribute, int Operator, string Value, bool IsNegated)[] clauses)
    {
        var conditions = new AuthorizationConditions();
        conditions.Grants[Code] = [new ConditionalGrantPath { Source = $"role:{RoleId}", GrantSetKeys = [$"rp:{RolePermissionId}"] }];
        conditions.Sets[$"rp:{RolePermissionId}"] = new AuthorizationConditionSet
        {
            Key = $"rp:{RolePermissionId}",
            PermissionCode = Code,
            Fingerprint = string.Join('|', clauses.Select(clause => clause.Attribute + clause.Value)),
            Clauses = [.. clauses.Select((clause, index) => new AuthorizationConditionClause
            {
                Id = index + 1,
                AttributeName = clause.Attribute,
                Operator = clause.Operator,
                Value = clause.Value,
                IsNegated = clause.IsNegated
            })]
        };
        _snapshot = _snapshot with { Permissions = [Code], Conditions = conditions };
    }

    private static PermissionConditionTrialInputDto Input(string? ip = null, DateTimeOffset? time = null)
    {
        return new PermissionConditionTrialInputDto { UserId = UserId, PermissionCode = Code, TenantId = TenantId, Ip = ip, Time = time ?? Now };
    }

    private static SysPermissionCondition Condition(long id, int group, string attribute, ConditionOperator @operator, string value, ConfigDataType valueType = ConfigDataType.String, ValidityStatus status = ValidityStatus.Valid)
    {
        var condition = new SysPermissionCondition
        {
            RolePermissionId = RolePermissionId,
            ConditionGroup = group,
            AttributeName = attribute,
            Operator = @operator,
            ConditionValue = value,
            ValueType = valueType,
            Status = status,
            TenantId = TenantId
        };
        SaasTestHelper.SetBasicId(condition, id);
        return condition;
    }

    private PermissionConditionInventoryService CreateInventory()
    {
        return new PermissionConditionInventoryService(
            _conditions.Object,
            _rolePermissions.Object,
            _userPermissions.Object,
            _roles.Object,
            _permissions.Object,
            _users.Object,
            _tenants.Object,
            _currentTenant,
            PermissionConditionTestKit.Compiler(_resources),
            new DefaultConditionAttributeRegistry(SaasConditionAttributes.All),
            _resources);
    }

    private PermissionConditionTrialService CreateTrial(HttpContext? current = null, params object[] rows)
    {
        var loader = new SaasConditionSubjectLoader(
            _snapshots.Object,
            new Mock<IUserDepartmentRepository>().Object,
            new Mock<IDepartmentRepository>().Object,
            new Mock<IPositionRepository>().Object,
            new Mock<ITenantUserRepository>().Object,
            _tenants.Object,
            _currentTenant);
        var accessor = new HttpContextAccessor { HttpContext = current };
        var sources = new SaasConditionAttributeSourceFactory(
            accessor,
            new Mock<XiHan.Framework.Security.Users.ICurrentUser>().Object,
            _currentTenant,
            loader,
            new Mock<IIpRegionResolver>().Object,
            Options.Create(new SaasConditionOptions()));
        var rowReader = new Mock<IPermissionResourceRowReader>();
        rowReader
            .Setup(reader => reader.ReadAsync(It.IsAny<PermissionResourceRegistration>(), It.IsAny<IReadOnlyCollection<long>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. rows]);
        return new PermissionConditionTrialService(
            _snapshots.Object,
            sources,
            PermissionConditionTestKit.Decider(_resources),
            _resources,
            rowReader.Object,
            _users.Object,
            _roles.Object,
            _userRoles.Object,
            _rolePermissions.Object,
            _userPermissions.Object,
            _permissions.Object,
            _hierarchy.Object,
            _currentTenant,
            accessor,
            Options.Create(new SaasConditionOptions()));
    }
}
