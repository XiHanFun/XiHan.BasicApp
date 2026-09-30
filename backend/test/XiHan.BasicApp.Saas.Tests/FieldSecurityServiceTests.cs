// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Linq.Expressions;
using Microsoft.Extensions.Options;
using Moq;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Models;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 字段级安全服务端测试：命中主体、规则合并、查询门控、读脱敏与写校验。
/// </summary>
public sealed class FieldSecurityServiceTests
{
    private const long UserId = 1;

    /// <summary>
    /// 未登录时没有规则。
    /// </summary>
    [Fact]
    public async Task Resolve_WithoutLogin_ShouldReturnEmpty()
    {
        var fixture = new Fixture(userId: null);

        var rules = await fixture.Service.ResolveAsync(typeof(SysUser));

        Assert.Empty(rules);
    }

    /// <summary>
    /// 未登记的实体当场报错，不因为没登录或没规则被悄悄放过。
    /// </summary>
    [Fact]
    public async Task Resolve_UnregisteredEntity_ShouldThrowEvenWithoutLogin()
    {
        var fixture = new Fixture(userId: null);

        _ = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ResolveAsync(typeof(SysRole)));
    }

    /// <summary>
    /// 命中：本人、生效且启用的角色、所在部门及其上级部门；其他人、停用角色、无关部门不命中。
    /// </summary>
    [Fact]
    public async Task Resolve_ShouldMatchUserRoleAndDepartmentTargets()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRoles(enabled: [10], disabled: [11]);
        fixture.SetupDepartments(memberOf: [21], (20, null, EnableStatus.Enabled), (21, 20, EnableStatus.Enabled), (30, null, EnableStatus.Enabled));
        fixture.SetupRules(
            Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Phone), FieldMaskStrategy.PartialMask, keepHead: 3, keepTail: 4),
            Rule(FieldSecurityTargetType.User, 2, nameof(SysUser.UserName), FieldMaskStrategy.Hidden),
            Rule(FieldSecurityTargetType.Role, 10, nameof(SysUser.Email), FieldMaskStrategy.Hidden),
            Rule(FieldSecurityTargetType.Role, 11, nameof(SysUser.RealName), FieldMaskStrategy.Hidden),
            Rule(FieldSecurityTargetType.Department, 20, nameof(SysUser.NickName), FieldMaskStrategy.FullMask),
            Rule(FieldSecurityTargetType.Department, 30, nameof(SysUser.Remark), FieldMaskStrategy.Hidden));

        var rules = await fixture.Service.ResolveAsync(typeof(SysUser));

        Assert.Equal(
            [nameof(SysUser.Email), nameof(SysUser.NickName), nameof(SysUser.Phone)],
            rules.Keys.Order(StringComparer.Ordinal));
    }

    /// <summary>
    /// 停用的上级部门不参与，但更上级的启用部门照样生效。
    /// </summary>
    [Fact]
    public async Task Resolve_DisabledAncestor_ShouldBeSkippedButWalkedThrough()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupDepartments(memberOf: [3], (1, null, EnableStatus.Enabled), (2, 1, EnableStatus.Disabled), (3, 2, EnableStatus.Enabled));
        fixture.SetupRules(
            Rule(FieldSecurityTargetType.Department, 1, nameof(SysUser.Phone), FieldMaskStrategy.Hidden),
            Rule(FieldSecurityTargetType.Department, 2, nameof(SysUser.Email), FieldMaskStrategy.Hidden));

        var rules = await fixture.Service.ResolveAsync(typeof(SysUser));

        Assert.Equal([nameof(SysUser.Phone)], rules.Keys);
    }

    /// <summary>
    /// 同一字段多条规则：读取方式取最严，部分脱敏保留位数取最少，任一只读即只读。
    /// </summary>
    [Fact]
    public async Task Resolve_ShouldMergeToStrictest()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRoles(enabled: [10, 11]);
        fixture.SetupRules(
            Rule(FieldSecurityTargetType.Role, 10, nameof(SysUser.Phone), FieldMaskStrategy.PartialMask, keepHead: 3, keepTail: 4),
            Rule(FieldSecurityTargetType.Role, 11, nameof(SysUser.Phone), FieldMaskStrategy.PartialMask, keepHead: 1, keepTail: 6),
            Rule(FieldSecurityTargetType.Role, 10, nameof(SysUser.Email), FieldMaskStrategy.Redact, replacement: "[保密]"),
            Rule(FieldSecurityTargetType.Role, 11, nameof(SysUser.Email), FieldMaskStrategy.FullMask),
            Rule(FieldSecurityTargetType.Role, 10, nameof(SysUser.NickName), FieldMaskStrategy.Hash),
            Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.NickName), FieldMaskStrategy.Hidden),
            Rule(FieldSecurityTargetType.Role, 10, nameof(SysUser.RealName), FieldMaskStrategy.None));

        var rules = await fixture.Service.ResolveAsync(typeof(SysUser));

        var phone = rules[nameof(SysUser.Phone)];
        Assert.Equal(FieldMaskStrategy.PartialMask, phone.MaskStrategy);
        Assert.Equal(1, phone.MaskKeepHead);
        Assert.Equal(4, phone.MaskKeepTail);
        Assert.Equal(FieldMaskStrategy.Redact, rules[nameof(SysUser.Email)].MaskStrategy);
        Assert.Equal("[保密]", rules[nameof(SysUser.Email)].MaskReplacement);
        Assert.Equal(FieldMaskStrategy.Hidden, rules[nameof(SysUser.NickName)].MaskStrategy);
        Assert.False(rules[nameof(SysUser.RealName)].IsEditable);
        Assert.False(rules[nameof(SysUser.RealName)].IsReadProtected);
        Assert.All(rules.Values, rule => Assert.False(rule.IsEditable));
    }

    /// <summary>
    /// 同一请求内按用户与实体只解析一次。
    /// </summary>
    [Fact]
    public async Task Resolve_ShouldCacheWithinScope()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Phone), FieldMaskStrategy.Hidden));

        _ = await fixture.Service.ResolveAsync(typeof(SysUser));
        _ = await fixture.Service.ResolveAsync(typeof(SysUser));

        fixture.Rules.Verify(repo => repo.GetEnabledByEntityAsync(nameof(SysUser), It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 查询门控剔除读受保护字段上的排序、过滤与关键字字段（大小写不敏感），明文只读字段不受影响。
    /// </summary>
    [Fact]
    public async Task GuardQuery_ShouldRemoveReadProtectedFieldsOnly()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(
            Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Phone), FieldMaskStrategy.PartialMask, keepHead: 3, keepTail: 4),
            Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.RealName), FieldMaskStrategy.None));
        var conditions = new QueryConditions()
            .AddSort("phone")
            .AddSort("realName")
            .AddFilter("Phone", "138")
            .AddFilter("UserName", "alice")
            .SetKeyword("138", nameof(SysUser.UserName), nameof(SysUser.Phone));

        await fixture.Service.GuardQueryAsync(conditions, typeof(SysUser));

        Assert.Equal(["realName"], conditions.Sorts.Select(sort => sort.Field));
        Assert.Equal(["UserName"], conditions.Filters.Select(filter => filter.Field));
        Assert.Equal([nameof(SysUser.UserName)], conditions.Keyword!.Fields);
    }

    /// <summary>
    /// 响应里的集合逐条脱敏；非文本字段按隐藏处理，回到类型默认值。
    /// </summary>
    [Fact]
    public async Task Mask_List_ShouldMaskEveryItem()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(
            Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Phone), FieldMaskStrategy.PartialMask, keepHead: 3, keepTail: 4),
            Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Gender), FieldMaskStrategy.Hidden),
            Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Birthday), FieldMaskStrategy.Hidden));
        var items = new List<UserRow>
        {
            new() { Phone = "13812345678", Gender = UserGender.Female, Birthday = DateTimeOffset.UnixEpoch },
            new() { Phone = "13987654321", Gender = UserGender.Male, Birthday = DateTimeOffset.UnixEpoch }
        };

        await fixture.Service.MaskAsync(items);

        Assert.Equal(["138****5678", "139****4321"], items.Select(item => item.Phone));
        Assert.All(items, item => Assert.Equal(default, item.Gender));
        Assert.All(items, item => Assert.Null(item.Birthday));
    }

    /// <summary>
    /// 沿对象图下行：分页里的行、聚合详情里嵌套的 DTO、字典的值都要打码；环不会死循环。
    /// </summary>
    [Fact]
    public async Task Mask_Graph_ShouldReachNestedDtos()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Email), FieldMaskStrategy.Hidden));
        var nested = new UserRow { UserName = "alice", Email = "alice@example.com" };
        var listed = new UserRow { UserName = "bob", Email = "bob@example.com" };
        var aggregate = new Aggregate
        {
            User = nested,
            Page = new PageResultDtoBase<UserRow>([listed], 1, 10, 1),
            Extra = new Dictionary<string, object> { ["user"] = new UserRow { UserName = "carol", Email = "carol@example.com" } }
        };
        aggregate.Self = aggregate;

        await fixture.Service.MaskAsync(aggregate);

        Assert.Null(nested.Email);
        Assert.Null(listed.Email);
        Assert.Null(((UserRow)aggregate.Extra["user"]).Email);
        Assert.Equal("alice", nested.UserName);
    }

    /// <summary>
    /// 不属于登记实体的对象原样放过，未登录时整份响应不动。
    /// </summary>
    [Fact]
    public async Task Mask_UnmappedOrAnonymous_ShouldLeaveUntouched()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Email), FieldMaskStrategy.Hidden));
        var unmapped = new UnmappedRow { Email = "alice@example.com" };
        var anonymous = new Fixture(userId: null);
        var row = new UserRow { Email = "alice@example.com" };

        await fixture.Service.MaskAsync(unmapped);
        await anonymous.Service.MaskAsync(row);

        Assert.Equal("alice@example.com", unmapped.Email);
        Assert.Equal("alice@example.com", row.Email);
    }

    /// <summary>
    /// 改名的 DTO 属性按声明的来源字段打码，同名属性不再误中。
    /// </summary>
    [Fact]
    public async Task Mask_RenamedProperty_ShouldFollowDeclaredSource()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Phone), FieldMaskStrategy.FullMask));
        var row = new RenamedRow { Mobile = "13812345678" };

        await fixture.Service.MaskAsync(row);

        Assert.Equal("***********", row.Mobile);
    }

    /// <summary>
    /// 多来源 DTO 按实例说明的来源实体与字段打码；来源不受管理的实例原样放过。
    /// </summary>
    [Fact]
    public async Task Mask_MultiSource_ShouldUseInstanceSource()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Email), FieldMaskStrategy.Hidden));
        var governed = new MultiSourceRow { Governed = true, Contact = "alice@example.com" };
        var ungoverned = new MultiSourceRow { Governed = false, Contact = "bob@example.com" };

        await fixture.Service.MaskAsync(new List<MultiSourceRow> { governed, ungoverned });

        Assert.Null(governed.Contact);
        Assert.Equal("bob@example.com", ungoverned.Contact);
    }

    /// <summary>
    /// 新建时只读字段不能填写。
    /// </summary>
    [Fact]
    public async Task EnsureCreatable_ReadOnlyFieldFilled_ShouldThrow()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Phone), FieldMaskStrategy.None));

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EnsureCreatableAsync(typeof(SysUser), new UserRow { UserName = "alice", Phone = "13812345678" }));
    }

    /// <summary>
    /// 新建时只读字段留空（含空串）可以通过，空串归一为默认值。
    /// </summary>
    [Fact]
    public async Task EnsureCreatable_ReadOnlyFieldLeftBlank_ShouldPassAndNormalize()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Phone), FieldMaskStrategy.Hidden));
        var input = new UserRow { UserName = "alice", Phone = string.Empty };

        await fixture.Service.EnsureCreatableAsync(typeof(SysUser), input);

        Assert.Null(input.Phone);
        Assert.Equal("alice", input.UserName);
    }

    /// <summary>
    /// 修改时只读字段原样交回可以通过。
    /// </summary>
    [Fact]
    public async Task EnsureUpdatable_ReadOnlyFieldUnchanged_ShouldPass()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.RealName), FieldMaskStrategy.None));
        fixture.SetupCurrent(new SysUser { UserName = "alice", RealName = "张三" });

        await fixture.Service.EnsureUpdatableAsync(typeof(SysUser), 100, new UserRow { UserName = "alice2", RealName = "张三" });
    }

    /// <summary>
    /// 修改时改动只读字段被拒绝。
    /// </summary>
    [Fact]
    public async Task EnsureUpdatable_ReadOnlyFieldChanged_ShouldThrow()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.RealName), FieldMaskStrategy.None));
        fixture.SetupCurrent(new SysUser { UserName = "alice", RealName = "张三" });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EnsureUpdatableAsync(typeof(SysUser), 100, new UserRow { UserName = "alice", RealName = "李四" }));
        Assert.Contains("无修改权限", exception.Message);
    }

    /// <summary>
    /// 表单把脱敏值原样交回：视为没改，并还原成原值，免得脱敏值覆盖真实数据。
    /// </summary>
    [Fact]
    public async Task EnsureUpdatable_MaskedValueRoundTrip_ShouldRestoreOriginal()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(
            Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Phone), FieldMaskStrategy.PartialMask, keepHead: 3, keepTail: 4),
            Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Email), FieldMaskStrategy.Hidden));
        fixture.SetupCurrent(new SysUser { UserName = "alice", Phone = "13812345678", Email = "alice@example.com" });
        var input = new UserRow { UserName = "alice", Phone = "138****5678", Email = null };

        await fixture.Service.EnsureUpdatableAsync(typeof(SysUser), 100, input);

        Assert.Equal("13812345678", input.Phone);
        Assert.Equal("alice@example.com", input.Email);
    }

    /// <summary>
    /// 脱敏字段交回别的值（想改）被拒绝。
    /// </summary>
    [Fact]
    public async Task EnsureUpdatable_MaskedFieldChanged_ShouldThrow()
    {
        var fixture = new Fixture(UserId);
        fixture.SetupRules(Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Phone), FieldMaskStrategy.PartialMask, keepHead: 3, keepTail: 4));
        fixture.SetupCurrent(new SysUser { UserName = "alice", Phone = "13812345678" });

        _ = await Assert.ThrowsAsync<InvalidOperationException>(
            () => fixture.Service.EnsureUpdatableAsync(typeof(SysUser), 100, new UserRow { UserName = "alice", Phone = "13900000000" }));
    }

    /// <summary>
    /// 只写字段（隐藏且可编辑）：交回空值视为没改并还原，填了新值照常保存。
    /// </summary>
    [Fact]
    public async Task EnsureUpdatable_WriteOnlyField_ShouldKeepOriginalOrAcceptNewValue()
    {
        var fixture = new Fixture(UserId);
        var rule = Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Email), FieldMaskStrategy.Hidden);
        rule.IsEditable = true;
        fixture.SetupRules(rule);
        fixture.SetupCurrent(new SysUser { UserName = "alice", Email = "alice@example.com" });
        var untouched = new UserRow { UserName = "alice", Email = null };
        var replaced = new UserRow { UserName = "alice", Email = "new@example.com" };

        await fixture.Service.EnsureUpdatableAsync(typeof(SysUser), 100, untouched);
        await fixture.Service.EnsureUpdatableAsync(typeof(SysUser), 100, replaced);

        Assert.Equal("alice@example.com", untouched.Email);
        Assert.Equal("new@example.com", replaced.Email);
    }

    /// <summary>
    /// 只写字段新建时可以填写。
    /// </summary>
    [Fact]
    public async Task EnsureCreatable_WriteOnlyField_ShouldAllowValue()
    {
        var fixture = new Fixture(UserId);
        var rule = Rule(FieldSecurityTargetType.User, UserId, nameof(SysUser.Email), FieldMaskStrategy.Hidden);
        rule.IsEditable = true;
        fixture.SetupRules(rule);
        var input = new UserRow { UserName = "alice", Email = "alice@example.com" };

        await fixture.Service.EnsureCreatableAsync(typeof(SysUser), input);

        Assert.Equal("alice@example.com", input.Email);
    }

    /// <summary>
    /// 没有只读规则时不读库。
    /// </summary>
    [Fact]
    public async Task EnsureUpdatable_WithoutReadOnlyRules_ShouldNotLoadCurrent()
    {
        var fixture = new Fixture(UserId);

        await fixture.Service.EnsureUpdatableAsync(typeof(SysUser), 100, new UserRow { UserName = "alice", Phone = "13900000000" });

        fixture.Reader.Verify(reader => reader.FindAsync(It.IsAny<Type>(), It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SysFieldLevelSecurity Rule(
        FieldSecurityTargetType targetType,
        long targetId,
        string fieldName,
        FieldMaskStrategy strategy,
        int? keepHead = null,
        int? keepTail = null,
        string? replacement = null)
    {
        return new SysFieldLevelSecurity
        {
            TargetType = targetType,
            TargetId = targetId,
            EntityName = nameof(SysUser),
            FieldName = fieldName,
            MaskStrategy = strategy,
            MaskKeepHead = keepHead,
            MaskKeepTail = keepTail,
            MaskReplacement = replacement,
            IsEditable = false,
            Status = EnableStatus.Enabled
        };
    }

    /// <summary>
    /// 与 SysUser 同名属性的返回/入参对象
    /// </summary>
    private sealed class UserRow
    {
        public string UserName { get; set; } = string.Empty;

        public string? RealName { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public UserGender Gender { get; set; }

        public DateTimeOffset? Birthday { get; set; }
    }

    /// <summary>
    /// 聚合响应：嵌套 DTO、分页、字典与自引用
    /// </summary>
    private sealed class Aggregate
    {
        public UserRow? User { get; set; }

        public PageResultDtoBase<UserRow>? Page { get; set; }

        public Dictionary<string, object> Extra { get; set; } = [];

        public Aggregate? Self { get; set; }
    }

    /// <summary>
    /// 与 SysUser 同名属性、但不属于任何登记实体的对象
    /// </summary>
    private sealed class UnmappedRow
    {
        public string? Email { get; set; }
    }

    /// <summary>
    /// 把 SysUser.Phone 改名为 Mobile 的 DTO
    /// </summary>
    private sealed class RenamedRow
    {
        [FieldSecuritySource(nameof(SysUser.Phone))]
        public string? Mobile { get; set; }
    }

    /// <summary>
    /// 多来源 DTO：Governed 为真时来自 SysUser，Contact 对应 Email
    /// </summary>
    private sealed class MultiSourceRow : IFieldSecurityMultiSourceDto
    {
        public bool Governed { get; set; }

        public string? Contact { get; set; }

        Type? IFieldSecurityMultiSourceDto.FieldSecurityEntity => Governed ? typeof(SysUser) : null;

        string? IFieldSecurityMultiSourceDto.FieldSecuritySourceOf(string dtoProperty) =>
            dtoProperty == nameof(Contact) ? nameof(SysUser.Email) : null;
    }

    /// <summary>
    /// 字段安全服务测试夹具：只登记 SysUser，UserRow 视为 SysUser 的 DTO
    /// </summary>
    private sealed class Fixture
    {
        public Fixture(long? userId)
        {
            var currentUser = new Mock<ICurrentUser>();
            currentUser.SetupGet(user => user.UserId).Returns(userId);
            UserRoles.Setup(repo => repo.GetValidByUserIdAsync(It.IsAny<long>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            UserDepartments.Setup(repo => repo.GetValidByUserIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);
            Rules.Setup(repo => repo.GetEnabledByEntityAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

            var catalog = new FieldSecurityEntityCatalog(Options.Create(new FieldSecurityEntityOptions().Add<SysUser>()));
            var dtoCatalog = new Mock<IFieldSecurityDtoCatalog>();
            dtoCatalog.Setup(value => value.EntityOf(It.IsAny<Type>())).Returns((Type type) => type == typeof(UserRow) || type == typeof(RenamedRow) ? typeof(SysUser) : null);
            Service = new FieldSecurityService(
                catalog,
                dtoCatalog.Object,
                Rules.Object,
                Reader.Object,
                UserRoles.Object,
                Roles.Object,
                UserDepartments.Object,
                Departments.Object,
                currentUser.Object);
        }

        public FieldSecurityService Service { get; }

        public Mock<IFieldLevelSecurityRepository> Rules { get; } = new();

        public Mock<IFieldSecurityEntityReader> Reader { get; } = new();

        public Mock<IUserRoleRepository> UserRoles { get; } = new();

        public Mock<IRoleRepository> Roles { get; } = new();

        public Mock<IUserDepartmentRepository> UserDepartments { get; } = new();

        public Mock<IDepartmentRepository> Departments { get; } = new();

        public void SetupRules(params SysFieldLevelSecurity[] rules)
        {
            for (var i = 0; i < rules.Length; i++)
            {
                SaasTestHelper.SetBasicId(rules[i], i + 1);
            }

            Rules.Setup(repo => repo.GetEnabledByEntityAsync(nameof(SysUser), It.IsAny<CancellationToken>())).ReturnsAsync(rules);
        }

        public void SetupRoles(long[] enabled, long[]? disabled = null)
        {
            var assigned = enabled.Concat(disabled ?? []).Select(roleId => new SysUserRole { UserId = UserId, RoleId = roleId }).ToArray();
            UserRoles.Setup(repo => repo.GetValidByUserIdAsync(UserId, It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>())).ReturnsAsync(assigned);
            Roles.Setup(repo => repo.GetEnabledByIdsAsync(It.IsAny<IEnumerable<long>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(enabled.Select(roleId =>
                {
                    var role = new SysRole { RoleCode = $"R{roleId}", RoleName = $"角色{roleId}", Status = EnableStatus.Enabled };
                    SaasTestHelper.SetBasicId(role, roleId);
                    return role;
                }).ToArray());
        }

        public void SetupDepartments(long[] memberOf, params (long Id, long? ParentId, EnableStatus Status)[] departments)
        {
            UserDepartments.Setup(repo => repo.GetValidByUserIdAsync(UserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(memberOf.Select(departmentId => new SysUserDepartment { UserId = UserId, DepartmentId = departmentId }).ToArray());
            Departments.Setup(repo => repo.GetListAsync(It.IsAny<Expression<Func<SysDepartment, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(departments.Select(item =>
                {
                    var department = new SysDepartment { DepartmentCode = $"D{item.Id}", DepartmentName = $"部门{item.Id}", ParentId = item.ParentId, Status = item.Status };
                    SaasTestHelper.SetBasicId(department, item.Id);
                    return department;
                }).ToArray());
        }

        public void SetupCurrent(SysUser current)
        {
            Reader.Setup(reader => reader.FindAsync(typeof(SysUser), It.IsAny<long>(), It.IsAny<CancellationToken>())).ReturnsAsync(current);
        }
    }
}
