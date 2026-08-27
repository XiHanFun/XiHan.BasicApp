// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Moq;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.MultiTenancy.Abstractions;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 权限 ABAC 条件领域服务测试：条件只能挂在「角色权限」或「用户直授权限」之一上，
/// 且绑定必须处于可用状态（有效、已生效、未过期、角色与权限均启用）；
/// 条件规模受「每绑定 5 组、每组 10 条」上限约束，同名属性的值类型必须一致。
/// </summary>
public sealed class SaasDomainPermissionConditionTests
{
    /// <summary>
    /// 条件必须且只能绑定一种授权：两个都填或都不填都要被拒。
    /// </summary>
    /// <param name="rolePermissionId">角色权限绑定主键。</param>
    /// <param name="userPermissionId">用户直授权限绑定主键。</param>
    [Theory]
    [InlineData(null, null)]
    [InlineData(1L, 2L)]
    public async Task CreatePermissionCondition_AmbiguousBinding_ShouldThrowInvalidOperationException(long? rolePermissionId, long? userPermissionId)
    {
        var context = new ConditionTestContext();
        var command = BuildCreateCommand(rolePermissionId: rolePermissionId, userPermissionId: userPermissionId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreatePermissionConditionAsync(command));

        Assert.Equal("ABAC 条件必须且只能绑定到角色权限或用户直授权限中的一种。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 绑定主键必须为正数，0 与负数一律拒绝。
    /// </summary>
    /// <param name="rolePermissionId">非法的角色权限绑定主键。</param>
    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public async Task CreatePermissionCondition_NonPositiveBindingId_ShouldThrowArgumentOutOfRange(long rolePermissionId)
    {
        var context = new ConditionTestContext();
        var command = BuildCreateCommand(rolePermissionId: rolePermissionId);

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePermissionConditionAsync(command));
    }

    /// <summary>
    /// 属性名必须使用 subject./resource./environment. 三个命名空间前缀之一，杜绝自造属性域。
    /// </summary>
    /// <param name="attributeName">属性名。</param>
    /// <param name="expectPass">期望是否通过前缀校验（通过则继续走后续绑定校验）。</param>
    [Theory]
    [InlineData("subject.department", true)]
    [InlineData("RESOURCE.Owner", true)]
    [InlineData("environment.ip", true)]
    [InlineData("subject", false)]
    [InlineData("user.department", false)]
    [InlineData("subjectx.department", false)]
    public async Task CreatePermissionCondition_AttributeNamePrefix_ShouldBeRestrictedToKnownNamespaces(string attributeName, bool expectPass)
    {
        var context = new ConditionTestContext();
        context.SetupUsableRolePermission();
        context.SetupNoExistingConditions();
        var command = BuildCreateCommand(attributeName: attributeName);

        if (expectPass)
        {
            var result = await context.Service.CreatePermissionConditionAsync(command);
            Assert.Equal(ConditionTestContext.SavedConditionId, result.ConditionId);
        }
        else
        {
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => context.Service.CreatePermissionConditionAsync(command));
            Assert.Equal("属性名称必须使用 subject./resource./environment. 命名空间前缀。", exception.Message, StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// 属性名与条件值为空白时直接拒绝（null 抛派生的空引用异常）。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_BlankAttributeOrValue_ShouldThrowArgumentException()
    {
        var context = new ConditionTestContext();

        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(attributeName: "   ")));
        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(attributeName: null!)));
        _ = await Assert.ThrowsAnyAsync<ArgumentException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(conditionValue: "   ")));
    }

    /// <summary>
    /// 条件分组序号不能为负数。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_NegativeConditionGroup_ShouldThrowArgumentOutOfRange()
    {
        var context = new ConditionTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(conditionGroup: -1)));
    }

    /// <summary>
    /// 属性名超过 200 字符、说明或备注超过 500 字符时拒绝。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_OverLongText_ShouldThrowArgumentOutOfRange()
    {
        var context = new ConditionTestContext();
        var longAttribute = "subject." + new string('a', 192);
        var overLongAttribute = "subject." + new string('a', 193);

        Assert.Equal(200, longAttribute.Length);
        Assert.Equal(201, overLongAttribute.Length);
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(attributeName: overLongAttribute)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(description: new string('d', 501))));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(remark: new string('r', 501))));

        // 恰好 200 字符的属性名应当放行，用于确认上界是「超过才拒」
        context.SetupUsableRolePermission();
        context.SetupNoExistingConditions();
        _ = await context.Service.CreatePermissionConditionAsync(BuildCreateCommand(attributeName: longAttribute));
    }

    /// <summary>
    /// 枚举入参必须在定义范围内，越界枚举值直接拒绝。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_UndefinedEnum_ShouldThrowArgumentOutOfRange()
    {
        var context = new ConditionTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePermissionConditionAsync(
                BuildCreateCommand(conditionOperator: (ConditionOperator)999)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePermissionConditionAsync(
                BuildCreateCommand(valueType: (ConfigDataType)999)));
        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.CreatePermissionConditionAsync(
                BuildCreateCommand(status: (ValidityStatus)999)));
    }

    /// <summary>
    /// 绑定的角色权限不存在时拒绝配置条件。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_MissingRolePermission_ShouldThrowInvalidOperationException()
    {
        var context = new ConditionTestContext();
        _ = context.RolePermissionRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysRolePermission?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand()));

        Assert.Equal("角色权限绑定不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 绑定状态无效、尚未生效、已过期三种情况分别给出可区分的拒绝提示。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_UnusableBindingPeriod_ShouldRejectWithDistinctMessages()
    {
        var invalidStatus = new ConditionTestContext();
        invalidStatus.SetupUsableRolePermission();
        invalidStatus.RolePermission.Status = ValidityStatus.Invalid;
        var invalidStatusException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => invalidStatus.Service.CreatePermissionConditionAsync(BuildCreateCommand()));
        Assert.Equal("无效角色权限绑定不能配置 ABAC 条件。", invalidStatusException.Message, StringComparer.Ordinal);

        var notYetEffective = new ConditionTestContext();
        notYetEffective.SetupUsableRolePermission();
        notYetEffective.RolePermission.EffectiveTime = DateTimeOffset.UtcNow.AddDays(1);
        var notYetEffectiveException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => notYetEffective.Service.CreatePermissionConditionAsync(BuildCreateCommand()));
        Assert.Equal("未生效授权绑定不能配置 ABAC 条件。", notYetEffectiveException.Message, StringComparer.Ordinal);

        var expired = new ConditionTestContext();
        expired.SetupUsableRolePermission();
        expired.RolePermission.ExpirationTime = DateTimeOffset.UtcNow.AddDays(-1);
        var expiredException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => expired.Service.CreatePermissionConditionAsync(BuildCreateCommand()));
        Assert.Equal("已过期授权绑定不能配置 ABAC 条件。", expiredException.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 角色或权限被停用时不允许配置条件，避免给已关停的授权继续加规则。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_DisabledRoleOrPermission_ShouldReject()
    {
        var disabledRole = new ConditionTestContext();
        disabledRole.SetupUsableRolePermission();
        disabledRole.Role.Status = EnableStatus.Disabled;
        var roleException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => disabledRole.Service.CreatePermissionConditionAsync(BuildCreateCommand()));
        Assert.Equal("停用角色不能配置 ABAC 条件。", roleException.Message, StringComparer.Ordinal);

        var disabledPermission = new ConditionTestContext();
        disabledPermission.SetupUsableRolePermission();
        disabledPermission.Permission.Status = EnableStatus.Disabled;
        var permissionException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => disabledPermission.Service.CreatePermissionConditionAsync(BuildCreateCommand()));
        Assert.Equal("停用权限不能配置 ABAC 条件。", permissionException.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 平台管理员成员的直授权限条件仅平台运维态可维护，租户态必须拒绝（跨租户越权防线）。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_PlatformAdminMemberInTenantContext_ShouldReject()
    {
        var context = new ConditionTestContext(currentTenantId: 7);
        context.SetupUsableUserPermission();
        context.TenantMember.MemberType = TenantMemberType.PlatformAdmin;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(rolePermissionId: null, userPermissionId: 20)));

        Assert.Contains("仅平台运维态可维护", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 平台运维态（当前租户为空或 0）下允许维护平台管理员成员的直授条件。
    /// </summary>
    /// <param name="currentTenantId">当前租户上下文标识。</param>
    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    public async Task CreatePermissionCondition_PlatformAdminMemberInPlatformContext_ShouldPass(long? currentTenantId)
    {
        var context = new ConditionTestContext(currentTenantId);
        context.SetupUsableUserPermission();
        context.SetupNoExistingConditions();
        context.TenantMember.MemberType = TenantMemberType.PlatformAdmin;

        var result = await context.Service.CreatePermissionConditionAsync(
            BuildCreateCommand(rolePermissionId: null, userPermissionId: 20));

        Assert.Equal(ConditionTestContext.SavedConditionId, result.ConditionId);
    }

    /// <summary>
    /// 租户成员缺失、未接受邀请、状态无效时都不允许配置直授条件。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_UnusableTenantMember_ShouldRejectWithDistinctMessages()
    {
        var missing = new ConditionTestContext();
        missing.SetupUsableUserPermission();
        _ = missing.TenantUserRepository
            .Setup(repo => repo.GetMembershipAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysTenantUser?)null);
        var missingException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => missing.Service.CreatePermissionConditionAsync(BuildCreateCommand(rolePermissionId: null, userPermissionId: 20)));
        Assert.Equal("当前租户成员不存在。", missingException.Message, StringComparer.Ordinal);

        var pending = new ConditionTestContext();
        pending.SetupUsableUserPermission();
        pending.TenantMember.InviteStatus = TenantMemberInviteStatus.Pending;
        var pendingException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => pending.Service.CreatePermissionConditionAsync(BuildCreateCommand(rolePermissionId: null, userPermissionId: 20)));
        Assert.Equal("未接受邀请的租户成员不能配置 ABAC 条件。", pendingException.Message, StringComparer.Ordinal);

        var invalid = new ConditionTestContext();
        invalid.SetupUsableUserPermission();
        invalid.TenantMember.Status = ValidityStatus.Invalid;
        var invalidException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => invalid.Service.CreatePermissionConditionAsync(BuildCreateCommand(rolePermissionId: null, userPermissionId: 20)));
        Assert.Equal("无效租户成员不能配置 ABAC 条件。", invalidException.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 租户成员未生效或已过期时同样拒绝配置直授条件。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_TenantMemberOutOfPeriod_ShouldReject()
    {
        var notYet = new ConditionTestContext();
        notYet.SetupUsableUserPermission();
        notYet.TenantMember.EffectiveTime = DateTimeOffset.UtcNow.AddDays(1);
        var notYetException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => notYet.Service.CreatePermissionConditionAsync(BuildCreateCommand(rolePermissionId: null, userPermissionId: 20)));
        Assert.Equal("未生效租户成员不能配置 ABAC 条件。", notYetException.Message, StringComparer.Ordinal);

        var expired = new ConditionTestContext();
        expired.SetupUsableUserPermission();
        expired.TenantMember.ExpirationTime = DateTimeOffset.UtcNow.AddDays(-1);
        var expiredException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => expired.Service.CreatePermissionConditionAsync(BuildCreateCommand(rolePermissionId: null, userPermissionId: 20)));
        Assert.Equal("已过期租户成员不能配置 ABAC 条件。", expiredException.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 单条授权绑定最多 5 个条件组：已占满 5 组时再开新组必须拒绝，写入已有组仍放行。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_ExceedingGroupLimit_ShouldReject()
    {
        var context = new ConditionTestContext();
        context.SetupUsableRolePermission();
        context.SetupExistingConditions(
            [.. Enumerable.Range(0, 5).Select(group => BuildCondition(group, "subject.department", ConfigDataType.String))]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(conditionGroup: 5)));
        Assert.Equal("单条授权绑定最多允许 5 个 ABAC 条件组。", exception.Message, StringComparer.Ordinal);

        // 落进已有分组不新增组数，应当放行
        _ = await context.Service.CreatePermissionConditionAsync(BuildCreateCommand(conditionGroup: 4));
    }

    /// <summary>
    /// 单个条件组最多 10 条条件：已有 10 条时第 11 条必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_ExceedingGroupItemLimit_ShouldReject()
    {
        var context = new ConditionTestContext();
        context.SetupUsableRolePermission();
        context.SetupExistingConditions(
            [.. Enumerable.Range(0, 10).Select(_ => BuildCondition(0, "subject.department", ConfigDataType.String))]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(conditionGroup: 0)));

        Assert.Equal("单个 ABAC 条件组最多允许 10 条条件。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 同一授权绑定内同名属性（忽略大小写）必须使用一致的值类型，跨组也要一致。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_ConflictingValueTypeForSameAttribute_ShouldReject()
    {
        var context = new ConditionTestContext();
        context.SetupUsableRolePermission();
        context.SetupExistingConditions([BuildCondition(1, "Subject.Department", ConfigDataType.Number)]);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.CreatePermissionConditionAsync(
                BuildCreateCommand(conditionGroup: 2, attributeName: "subject.department", valueType: ConfigDataType.String)));

        Assert.Equal("同一授权绑定内相同 ABAC 属性必须使用一致的值类型。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 落库前会对属性名、条件值做去空白处理，说明与备注的纯空白折叠为 null。
    /// </summary>
    [Fact]
    public async Task CreatePermissionCondition_ShouldTrimTextAndNormalizeBlankOptionalFields()
    {
        var context = new ConditionTestContext();
        context.SetupUsableRolePermission();
        context.SetupNoExistingConditions();

        _ = await context.Service.CreatePermissionConditionAsync(BuildCreateCommand(
            attributeName: "  subject.department  ",
            conditionValue: "  10  ",
            description: "   ",
            remark: "  备注  "));

        Assert.NotNull(context.SavedCondition);
        Assert.Equal("subject.department", context.SavedCondition!.AttributeName, StringComparer.Ordinal);
        Assert.Equal("10", context.SavedCondition.ConditionValue, StringComparer.Ordinal);
        Assert.Null(context.SavedCondition.Description);
        Assert.Equal("备注", context.SavedCondition.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新时必须携带正数主键，非法主键在任何查库动作之前就被拒绝。
    /// </summary>
    [Fact]
    public async Task UpdatePermissionCondition_NonPositiveId_ShouldThrowArgumentOutOfRange()
    {
        var context = new ConditionTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.UpdatePermissionConditionAsync(new PermissionConditionUpdateCommand(
                0, 10, null, 0, "subject.department", ConditionOperator.Equals, false, ConfigDataType.String, "10", null, null)));
        context.ConditionRepository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 更新目标不存在时拒绝。
    /// </summary>
    [Fact]
    public async Task UpdatePermissionCondition_MissingCondition_ShouldThrowInvalidOperationException()
    {
        var context = new ConditionTestContext();
        _ = context.ConditionRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPermissionCondition?)null);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.UpdatePermissionConditionAsync(new PermissionConditionUpdateCommand(
                5, 10, null, 0, "subject.department", ConditionOperator.Equals, false, ConfigDataType.String, "10", null, null)));

        Assert.Equal("权限 ABAC 条件不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新自身时不得把自己算进上限，否则组内刚好满 10 条的条件将永远无法编辑。
    /// </summary>
    [Fact]
    public async Task UpdatePermissionCondition_ShouldExcludeItselfFromLimitCheck()
    {
        var context = new ConditionTestContext();
        context.SetupUsableRolePermission();
        var existing = Enumerable.Range(0, 10)
            .Select(_ => BuildCondition(0, "subject.department", ConfigDataType.String))
            .ToArray();
        SaasTestHelper.SetBasicId(existing[0], 77);
        context.SetupExistingConditions(existing);
        _ = context.ConditionRepository
            .Setup(repo => repo.GetByIdAsync(77, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing[0]);

        var result = await context.Service.UpdatePermissionConditionAsync(new PermissionConditionUpdateCommand(
            77, 10, null, 0, "subject.department", ConditionOperator.Equals, false, ConfigDataType.String, "20", null, null));

        Assert.Equal(77, result.ConditionId);
        Assert.Equal("20", existing[0].ConditionValue, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态置为无效时无需重新校验绑定可用性：条件已停用不再影响鉴权，不能被失效绑定卡住。
    /// </summary>
    [Fact]
    public async Task UpdatePermissionConditionStatus_ToInvalid_ShouldSkipBindingRevalidation()
    {
        var context = new ConditionTestContext();
        var condition = BuildCondition(0, "subject.department", ConfigDataType.String);
        SaasTestHelper.SetBasicId(condition, 88);
        condition.RolePermissionId = 10;
        _ = context.ConditionRepository
            .Setup(repo => repo.GetByIdAsync(88, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condition);

        var result = await context.Service.UpdatePermissionConditionStatusAsync(
            new PermissionConditionStatusCommand(88, ValidityStatus.Invalid, "停用"));

        Assert.Equal(88, result.ConditionId);
        Assert.Equal(ValidityStatus.Invalid, condition.Status);
        Assert.Equal("停用", condition.Remark, StringComparer.Ordinal);
        context.RolePermissionRepository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 状态置为有效时必须重新校验绑定可用性，失效绑定不得被重新启用条件。
    /// </summary>
    [Fact]
    public async Task UpdatePermissionConditionStatus_ToValid_ShouldRevalidateBinding()
    {
        var context = new ConditionTestContext();
        context.SetupUsableRolePermission();
        context.RolePermission.Status = ValidityStatus.Invalid;
        var condition = BuildCondition(0, "subject.department", ConfigDataType.String);
        SaasTestHelper.SetBasicId(condition, 88);
        condition.RolePermissionId = 10;
        _ = context.ConditionRepository
            .Setup(repo => repo.GetByIdAsync(88, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condition);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.UpdatePermissionConditionStatusAsync(
                new PermissionConditionStatusCommand(88, ValidityStatus.Valid, null)));

        Assert.Equal("无效角色权限绑定不能配置 ABAC 条件。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 状态命令的备注为空白时保留原备注，不得把历史备注抹成 null。
    /// </summary>
    [Fact]
    public async Task UpdatePermissionConditionStatus_BlankRemark_ShouldKeepExistingRemark()
    {
        var context = new ConditionTestContext();
        var condition = BuildCondition(0, "subject.department", ConfigDataType.String);
        SaasTestHelper.SetBasicId(condition, 88);
        condition.RolePermissionId = 10;
        condition.Remark = "原备注";
        _ = context.ConditionRepository
            .Setup(repo => repo.GetByIdAsync(88, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condition);

        _ = await context.Service.UpdatePermissionConditionStatusAsync(
            new PermissionConditionStatusCommand(88, ValidityStatus.Invalid, "   "));

        Assert.Equal("原备注", condition.Remark, StringComparer.Ordinal);
    }

    /// <summary>
    /// 删除必须携带正数主键；目标不存在或仓储返回失败时给出可区分的拒绝。
    /// </summary>
    [Fact]
    public async Task DeletePermissionCondition_ShouldRejectInvalidIdMissingTargetAndFailedDelete()
    {
        var context = new ConditionTestContext();

        _ = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => context.Service.DeletePermissionConditionAsync(0));

        _ = context.ConditionRepository
            .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysPermissionCondition?)null);
        var missingException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.DeletePermissionConditionAsync(9));
        Assert.Equal("权限 ABAC 条件不存在。", missingException.Message, StringComparer.Ordinal);

        var condition = BuildCondition(0, "subject.department", ConfigDataType.String);
        _ = context.ConditionRepository
            .Setup(repo => repo.GetByIdAsync(9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(condition);
        _ = context.ConditionRepository
            .Setup(repo => repo.DeleteAsync(condition, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var failedException = await Assert.ThrowsAsync<InvalidOperationException>(
            () => context.Service.DeletePermissionConditionAsync(9));
        Assert.Equal("权限 ABAC 条件删除失败。", failedException.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 命令对象为空是调用方缺陷，必须抛空引用异常。
    /// </summary>
    [Fact]
    public async Task PermissionConditionCommands_NullCommand_ShouldThrowArgumentNullException()
    {
        var context = new ConditionTestContext();

        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.Service.CreatePermissionConditionAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.Service.UpdatePermissionConditionAsync(null!));
        _ = await Assert.ThrowsAsync<ArgumentNullException>(
            () => context.Service.UpdatePermissionConditionStatusAsync(null!));
    }

    /// <summary>
    /// 已取消的令牌必须在任何仓储访问之前抛出取消异常。
    /// </summary>
    [Fact]
    public async Task PermissionConditionCommands_CancelledToken_ShouldThrowBeforeRepositoryCall()
    {
        var context = new ConditionTestContext();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.CreatePermissionConditionAsync(BuildCreateCommand(), cancellation.Token));
        _ = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => context.Service.DeletePermissionConditionAsync(1, cancellation.Token));
        context.ConditionRepository.Verify(
            repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        context.ConditionRepository.Verify(
            repo => repo.AddAsync(It.IsAny<SysPermissionCondition>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static SysPermissionCondition BuildCondition(int conditionGroup, string attributeName, ConfigDataType valueType)
    {
        return new SysPermissionCondition
        {
            RolePermissionId = 10,
            ConditionGroup = conditionGroup,
            AttributeName = attributeName,
            ValueType = valueType,
            ConditionValue = "10",
            Status = ValidityStatus.Valid
        };
    }

    private static PermissionConditionCreateCommand BuildCreateCommand(
        long? rolePermissionId = 10,
        long? userPermissionId = null,
        int conditionGroup = 0,
        string attributeName = "subject.department",
        ConditionOperator conditionOperator = ConditionOperator.Equals,
        ConfigDataType valueType = ConfigDataType.String,
        string conditionValue = "10",
        string? description = null,
        ValidityStatus status = ValidityStatus.Valid,
        string? remark = null)
    {
        return new PermissionConditionCreateCommand(
            rolePermissionId,
            userPermissionId,
            conditionGroup,
            attributeName,
            conditionOperator,
            false,
            valueType,
            conditionValue,
            description,
            status,
            remark);
    }

    /// <summary>
    /// 权限 ABAC 条件领域服务的依赖装配夹具：默认全部仓储返回空，按用例逐条打开可用路径。
    /// </summary>
    private sealed class ConditionTestContext
    {
        internal const long SavedConditionId = 999;

        internal ConditionTestContext(long? currentTenantId = null)
        {
            ConditionRepository = new Mock<IPermissionConditionRepository>();
            RolePermissionRepository = new Mock<IRolePermissionRepository>();
            UserPermissionRepository = new Mock<IUserPermissionRepository>();
            RoleRepository = new Mock<IRoleRepository>();
            PermissionRepository = new Mock<IPermissionRepository>();
            TenantUserRepository = new Mock<ITenantUserRepository>();
            CurrentTenant = new Mock<ICurrentTenant>();
            _ = CurrentTenant.SetupGet(tenant => tenant.Id).Returns(currentTenantId);

            _ = ConditionRepository
                .Setup(repo => repo.AddAsync(It.IsAny<SysPermissionCondition>(), It.IsAny<CancellationToken>()))
                .Callback<SysPermissionCondition, CancellationToken>((entity, _) =>
                {
                    SavedCondition = entity;
                    SaasTestHelper.SetBasicId(entity, SavedConditionId);
                })
                .ReturnsAsync((SysPermissionCondition entity, CancellationToken _) => entity);
            _ = ConditionRepository
                .Setup(repo => repo.UpdateAsync(It.IsAny<SysPermissionCondition>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SysPermissionCondition entity, CancellationToken _) => entity);

            Service = new PermissionConditionDomainService(
                ConditionRepository.Object,
                RolePermissionRepository.Object,
                UserPermissionRepository.Object,
                RoleRepository.Object,
                PermissionRepository.Object,
                TenantUserRepository.Object,
                CurrentTenant.Object);
        }

        internal Mock<IPermissionConditionRepository> ConditionRepository { get; }

        internal Mock<ICurrentTenant> CurrentTenant { get; }

        internal SysPermission Permission { get; } = new() { Status = EnableStatus.Enabled };

        internal Mock<IPermissionRepository> PermissionRepository { get; }

        internal SysRole Role { get; } = new() { Status = EnableStatus.Enabled };

        internal SysRolePermission RolePermission { get; } = new() { RoleId = 1, PermissionId = 2, Status = ValidityStatus.Valid };

        internal Mock<IRolePermissionRepository> RolePermissionRepository { get; }

        internal Mock<IRoleRepository> RoleRepository { get; }

        internal SysPermissionCondition? SavedCondition { get; private set; }

        internal PermissionConditionDomainService Service { get; }

        internal SysTenantUser TenantMember { get; } = new()
        {
            UserId = 3,
            MemberType = TenantMemberType.Member,
            InviteStatus = TenantMemberInviteStatus.Accepted,
            Status = ValidityStatus.Valid
        };

        internal Mock<ITenantUserRepository> TenantUserRepository { get; }

        internal SysUserPermission UserPermission { get; } = new() { UserId = 3, PermissionId = 2, Status = ValidityStatus.Valid };

        internal Mock<IUserPermissionRepository> UserPermissionRepository { get; }

        internal void SetupExistingConditions(IReadOnlyList<SysPermissionCondition> conditions)
        {
            _ = ConditionRepository
                .Setup(repo => repo.GetListAsync(
                    It.IsAny<Expression<Func<SysPermissionCondition, bool>>>(),
                    It.IsAny<Expression<Func<SysPermissionCondition, object>>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(conditions);
        }

        internal void SetupNoExistingConditions()
        {
            SetupExistingConditions([]);
        }

        internal void SetupUsableRolePermission()
        {
            _ = RolePermissionRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(RolePermission);
            _ = RoleRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Role);
            _ = PermissionRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Permission);
        }

        internal void SetupUsableUserPermission()
        {
            _ = UserPermissionRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(UserPermission);
            _ = TenantUserRepository
                .Setup(repo => repo.GetMembershipAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(TenantMember);
            _ = PermissionRepository
                .Setup(repo => repo.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Permission);
        }
    }
}
