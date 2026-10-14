// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using System.Linq.Expressions;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Workflow.Infrastructure.Assignees;
using XiHan.Framework.Workflow.Abstractions.Assignees;
using XiHan.Framework.Workflow.Abstractions.Definitions;
using XiHan.Framework.Workflow.Abstractions.Runtime;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 组织架构受理人解析器测试（角色、部门负责人、发起人主管链）。
/// </summary>
/// <remarks>
/// 组织：总部（1，负责人 9001）→ 研发部（2，负责人 9002）→ 前端组（3，负责人 9003）。
/// 发起人 9003 自己就是前端组负责人，其直属主管应是研发部负责人 9002，而不是自己。
/// </remarks>
public sealed class OrganizationAssigneeProviderTests
{
    private readonly Mock<IUserDirectory> _userDirectory = new();
    private readonly Mock<IRoleRepository> _roles = new();
    private readonly Mock<IUserRoleRepository> _userRoles = new();
    private readonly Mock<IDepartmentRepository> _departments = new();
    private readonly Mock<IUserDepartmentRepository> _userDepartments = new();
    private readonly Mock<IPositionRepository> _positions = new();

    /// <summary>
    /// 构造函数：搭好三级部门与启用账号
    /// </summary>
    public OrganizationAssigneeProviderTests()
    {
        _userDirectory
            .Setup(value => value.GetEnabledIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([9001L, 9002L, 9003L, 9100L]);

        var headquarters = CreateDepartment(1L, null, "hq", 9001L);
        var research = CreateDepartment(2L, 1L, "rd", 9002L);
        var frontend = CreateDepartment(3L, 2L, "fe", 9003L);
        _departments.Setup(value => value.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(headquarters);
        _departments.Setup(value => value.GetByIdAsync(2L, It.IsAny<CancellationToken>())).ReturnsAsync(research);
        _departments.Setup(value => value.GetByIdAsync(3L, It.IsAny<CancellationToken>())).ReturnsAsync(frontend);
        _departments
            .Setup(value => value.GetListAsync(It.IsAny<Expression<Func<SysDepartment, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expression<Func<SysDepartment, bool>> predicate, CancellationToken _) =>
                [.. new[] { headquarters, research, frontend }.Where(predicate.Compile())]);
    }

    /// <summary>
    /// 发起人是本部门负责人时，直属主管取上一级部门的负责人。
    /// </summary>
    [Fact]
    public async Task StarterLeader_StarterLeadsOwnDepartment_ShouldSkipToParentLeader()
    {
        SetMainDepartment(9003L, 3L);

        var assignees = await new StarterLeaderAssigneeProvider(CreateDirectory()).ResolveAsync(CreateContext(OrganizationAssigneeRuleTypes.StarterLeader, "9003"));

        Assert.Equal(["9002"], assignees);
    }

    /// <summary>
    /// 第 N 级主管超出组织层级时为空（交给受理人为空策略处理），而不是退回到更低层级。
    /// </summary>
    [Fact]
    public async Task StarterLeader_LevelBeyondChain_ShouldBeEmpty()
    {
        SetMainDepartment(9003L, 3L);

        var assignees = await new StarterLeaderAssigneeProvider(CreateDirectory()).ResolveAsync(CreateContext(OrganizationAssigneeRuleTypes.StarterLeader, "9003", "3"));

        Assert.Empty(assignees);
    }

    /// <summary>
    /// 逐级主管链按层级顺序返回（配合依次审批），停用账号跳过。
    /// </summary>
    [Fact]
    public async Task StarterLeaderChain_ShouldReturnOrderedChainSkippingDisabled()
    {
        SetMainDepartment(9100L, 3L);
        _userDirectory
            .Setup(value => value.GetEnabledIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([9001L, 9003L, 9100L]);

        var assignees = await new StarterLeaderChainAssigneeProvider(CreateDirectory()).ResolveAsync(CreateContext(OrganizationAssigneeRuleTypes.StarterLeaderChain, "9100", "3"));

        Assert.Equal(["9003", "9001"], assignees);
    }

    /// <summary>
    /// 主管层级参数只接受 1-10 的整数。
    /// </summary>
    /// <param name="level">层级参数。</param>
    /// <param name="valid">是否合法。</param>
    [Theory]
    [InlineData("1", true)]
    [InlineData("10", true)]
    [InlineData("0", false)]
    [InlineData("11", false)]
    [InlineData("x", false)]
    public void StarterLeader_Validate_ShouldAcceptOneToTen(string level, bool valid)
    {
        var errors = new StarterLeaderAssigneeProvider(CreateDirectory()).Validate(new WorkflowAssigneeRule
        {
            Type = OrganizationAssigneeRuleTypes.StarterLeader,
            Values = [level]
        });

        Assert.Equal(valid, errors.Count == 0);
    }

    /// <summary>
    /// 部门负责人按编码顺序返回，只取启用账号。
    /// </summary>
    [Fact]
    public async Task DepartmentLeader_ShouldFollowCodeOrder()
    {
        var assignees = await new DepartmentLeaderAssigneeProvider(CreateDirectory()).ResolveAsync(
            CreateContext(OrganizationAssigneeRuleTypes.DepartmentLeader, "9100", "fe", "hq"));

        Assert.Equal(["9003", "9001"], assignees);
    }

    /// <summary>
    /// 角色成员只取启用角色的有效授予，并与启用账号求交。
    /// </summary>
    [Fact]
    public async Task Role_ShouldResolveValidMembersOfEnabledRoles()
    {
        var role = new SysRole { RoleCode = "finance", RoleName = "财务", Status = EnableStatus.Enabled };
        WorkflowTestHelper.SetBasicId(role, 501L);
        _roles
            .Setup(value => value.GetListAsync(It.IsAny<Expression<Func<SysRole, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([role]);
        _userRoles
            .Setup(value => value.GetValidByRoleIdsAsync(It.Is<IReadOnlyCollection<long>>(ids => ids.Single() == 501L), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SysUserRole { UserId = 9002L, RoleId = 501L }, new SysUserRole { UserId = 8888L, RoleId = 501L }]);

        var assignees = await new RoleAssigneeProvider(CreateDirectory()).ResolveAsync(CreateContext(OrganizationAssigneeRuleTypes.Role, "9100", "finance"));

        Assert.Equal(["9002"], assignees);
    }

    /// <summary>
    /// 编码类规则至少要一个编码。
    /// </summary>
    [Fact]
    public void Role_ValidateWithoutCodes_ShouldFail()
    {
        var errors = new RoleAssigneeProvider(CreateDirectory()).Validate(new WorkflowAssigneeRule { Type = OrganizationAssigneeRuleTypes.Role });

        Assert.NotEmpty(errors);
    }

    private WorkflowOrganizationDirectory CreateDirectory()
    {
        return new WorkflowOrganizationDirectory(
            _userDirectory.Object,
            _roles.Object,
            _userRoles.Object,
            _departments.Object,
            _userDepartments.Object,
            _positions.Object);
    }

    private void SetMainDepartment(long userId, long departmentId)
    {
        _userDepartments
            .Setup(value => value.GetValidByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([new SysUserDepartment { UserId = userId, DepartmentId = departmentId, IsMain = true }]);
    }

    private static SysDepartment CreateDepartment(long id, long? parentId, string code, long leaderId)
    {
        var department = new SysDepartment
        {
            ParentId = parentId,
            DepartmentCode = code,
            DepartmentName = code,
            LeaderId = leaderId,
            Status = EnableStatus.Enabled
        };
        WorkflowTestHelper.SetBasicId(department, id);
        return department;
    }

    private static WorkflowAssigneeResolveContext CreateContext(string ruleType, string starterId, params string[] values)
    {
        var instance = WorkflowTestHelper.CreateInstance(status: WorkflowInstanceStatus.Running, parentInstanceId: null);
        instance.StarterId = starterId;
        return new WorkflowAssigneeResolveContext
        {
            Rule = new WorkflowAssigneeRule { Type = ruleType, Values = [.. values] },
            Definition = WorkflowTestHelper.CreateDefinition(),
            Instance = instance,
            Node = new WorkflowNode { Id = "approve", ActivityType = "UserTask" },
            Variables = new Dictionary<string, object?>()
        };
    }
}
