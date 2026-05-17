#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentDomainService
// Guid:7e4f5e15-a137-44af-a217-19f5f24382e0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 部门领域服务实现
/// </summary>
public sealed class DepartmentDomainService
    : IDepartmentDomainService
{
    private readonly IDepartmentRepository _departmentRepository;

    private readonly IDepartmentHierarchyRepository _departmentHierarchyRepository;

    private readonly ITenantUserRepository _tenantUserRepository;

    private readonly IUserDepartmentRepository _userDepartmentRepository;

    private readonly IRoleDataScopeRepository _roleDataScopeRepository;

    private readonly IUserDataScopeRepository _userDataScopeRepository;

    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public DepartmentDomainService(
        IDepartmentRepository departmentRepository,
        IDepartmentHierarchyRepository departmentHierarchyRepository,
        ITenantUserRepository tenantUserRepository,
        IUserDepartmentRepository userDepartmentRepository,
        IRoleDataScopeRepository roleDataScopeRepository,
        IUserDataScopeRepository userDataScopeRepository,
        IFieldLevelSecurityRepository fieldLevelSecurityRepository)
    {
        _departmentRepository = departmentRepository;
        _departmentHierarchyRepository = departmentHierarchyRepository;
        _tenantUserRepository = tenantUserRepository;
        _userDepartmentRepository = userDepartmentRepository;
        _roleDataScopeRepository = roleDataScopeRepository;
        _userDataScopeRepository = userDataScopeRepository;
        _fieldLevelSecurityRepository = fieldLevelSecurityRepository;
    }

    /// <inheritdoc />
    public async Task<DepartmentCommandResult> CreateAsync(DepartmentCreateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateCommand(command);

        var departmentCode = command.DepartmentCode.Trim();
        if (await _departmentRepository.GetByCodeAsync(departmentCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("部门编码已存在。");
        }

        _ = await ValidateParentAsync(command.ParentId, currentDepartmentId: null, command.Status == EnableStatus.Enabled, cancellationToken);
        await ValidateLeaderAsync(command.LeaderId, cancellationToken);

        var department = new SysDepartment
        {
            ParentId = command.ParentId,
            DepartmentName = command.DepartmentName.Trim(),
            DepartmentCode = departmentCode,
            DepartmentType = command.DepartmentType,
            LeaderId = command.LeaderId,
            Phone = NormalizeNullable(command.Phone),
            Email = NormalizeNullable(command.Email),
            Address = NormalizeNullable(command.Address),
            Status = command.Status,
            Sort = command.Sort,
            Remark = NormalizeNullable(command.Remark)
        };

        var savedDepartment = await _departmentRepository.AddAsync(department, cancellationToken);
        await RebuildDepartmentHierarchyAsync(cancellationToken);

        return new DepartmentCommandResult(savedDepartment);
    }

    /// <inheritdoc />
    public async Task<DepartmentCommandResult> UpdateAsync(DepartmentUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateCommand(command);

        var department = await GetDepartmentOrThrowAsync(command.BasicId, cancellationToken);
        var parentChanged = department.ParentId != command.ParentId;

        _ = await ValidateParentAsync(command.ParentId, department.BasicId, department.Status == EnableStatus.Enabled, cancellationToken);
        await ValidateLeaderAsync(command.LeaderId, cancellationToken);

        department.ParentId = command.ParentId;
        department.DepartmentName = command.DepartmentName.Trim();
        department.DepartmentType = command.DepartmentType;
        department.LeaderId = command.LeaderId;
        department.Phone = NormalizeNullable(command.Phone);
        department.Email = NormalizeNullable(command.Email);
        department.Address = NormalizeNullable(command.Address);
        department.Sort = command.Sort;
        department.Remark = NormalizeNullable(command.Remark);

        var savedDepartment = await _departmentRepository.UpdateAsync(department, cancellationToken);
        if (parentChanged)
        {
            await RebuildDepartmentHierarchyAsync(cancellationToken);
        }

        return new DepartmentCommandResult(savedDepartment);
    }

    /// <inheritdoc />
    public async Task<DepartmentCommandResult> UpdateStatusAsync(DepartmentStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "部门主键必须大于 0。");
        }

        ValidateEnum(command.Status, nameof(command.Status));

        var department = await GetDepartmentOrThrowAsync(command.BasicId, cancellationToken);
        if (command.Status == EnableStatus.Enabled)
        {
            _ = await ValidateParentAsync(department.ParentId, department.BasicId, requireEnabledParent: true, cancellationToken);
            await ValidateLeaderAsync(department.LeaderId, cancellationToken);
        }
        else if (await _departmentRepository.AnyAsync(
            child => child.ParentId == department.BasicId && child.Status == EnableStatus.Enabled,
            cancellationToken))
        {
            throw new InvalidOperationException("部门存在已启用子部门，不能直接停用。");
        }

        department.Status = command.Status;
        department.Remark = NormalizeNullable(command.Remark) ?? department.Remark;

        var savedDepartment = await _departmentRepository.UpdateAsync(department, cancellationToken);
        return new DepartmentCommandResult(savedDepartment);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var department = await GetDepartmentOrThrowAsync(id, cancellationToken);
        await EnsureCanDeleteDepartmentAsync(department, cancellationToken);

        if (!await _departmentRepository.DeleteAsync(department, cancellationToken))
        {
            throw new InvalidOperationException("部门删除失败。");
        }

        await RebuildDepartmentHierarchyAsync(cancellationToken);
    }

    private static IReadOnlyList<SysDepartmentHierarchy> BuildHierarchyRows(IReadOnlyList<SysDepartment> departments)
    {
        var departmentMap = departments.ToDictionary(department => department.BasicId);
        var rows = new List<SysDepartmentHierarchy>();

        foreach (var department in departments.OrderBy(department => department.ParentId ?? 0).ThenBy(department => department.Sort).ThenBy(department => department.DepartmentCode, StringComparer.Ordinal))
        {
            var chain = BuildAncestorChain(department, departmentMap);
            for (var depth = 0; depth < chain.Count; depth++)
            {
                var ancestor = chain[depth];
                var pathNodes = chain.Take(depth + 1).Reverse().ToArray();
                rows.Add(new SysDepartmentHierarchy
                {
                    AncestorId = ancestor.BasicId,
                    DescendantId = department.BasicId,
                    Depth = depth,
                    Path = string.Join("/", pathNodes.Select(node => node.BasicId)),
                    PathName = string.Join("/", pathNodes.Select(node => node.DepartmentName))
                });
            }
        }

        return rows;
    }

    private static IReadOnlyList<SysDepartment> BuildAncestorChain(SysDepartment department, IReadOnlyDictionary<long, SysDepartment> departmentMap)
    {
        var chain = new List<SysDepartment>();
        var visited = new HashSet<long>();
        var cursor = department;

        while (true)
        {
            if (!visited.Add(cursor.BasicId))
            {
                throw new InvalidOperationException("部门层级存在环路，不能重建闭包表。");
            }

            chain.Add(cursor);
            if (!cursor.ParentId.HasValue)
            {
                return chain;
            }

            if (!departmentMap.TryGetValue(cursor.ParentId.Value, out var parent))
            {
                throw new InvalidOperationException("部门层级存在缺失父级，不能重建闭包表。");
            }

            cursor = parent;
        }
    }

    private static void ValidateCreateCommand(DepartmentCreateCommand command)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(command.DepartmentCode);
        ValidateDepartmentCode(command.DepartmentCode);
        ValidateCommonCommand(
            command.DepartmentName,
            command.DepartmentType,
            command.Phone,
            command.Email,
            command.Address,
            command.Remark);
        ValidateEnum(command.Status, nameof(command.Status));
    }

    private static void ValidateUpdateCommand(DepartmentUpdateCommand command)
    {
        if (command.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(command), "部门主键必须大于 0。");
        }

        ValidateCommonCommand(
            command.DepartmentName,
            command.DepartmentType,
            command.Phone,
            command.Email,
            command.Address,
            command.Remark);
    }

    private static void ValidateCommonCommand(
        string departmentName,
        DepartmentType departmentType,
        string? phone,
        string? email,
        string? address,
        string? remark)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(departmentName);
        ValidateEnum(departmentType, nameof(departmentType));
        ValidateLength(departmentName, 100, nameof(departmentName), "部门名称不能超过 100 个字符。");
        ValidateOptionalLength(phone, 20, nameof(phone), "联系电话不能超过 20 个字符。");
        ValidateOptionalLength(email, 100, nameof(email), "邮箱不能超过 100 个字符。");
        ValidateOptionalLength(address, 500, nameof(address), "地址不能超过 500 个字符。");
        ValidateOptionalLength(remark, 500, nameof(remark), "备注不能超过 500 个字符。");
    }

    private static void ValidateDepartmentCode(string departmentCode)
    {
        var normalizedDepartmentCode = departmentCode.Trim();
        ValidateLength(normalizedDepartmentCode, 100, nameof(departmentCode), "部门编码不能超过 100 个字符。");
        if (normalizedDepartmentCode.Any(char.IsWhiteSpace))
        {
            throw new InvalidOperationException("部门编码不能包含空白字符。");
        }

        if (normalizedDepartmentCode.Any(static code => !IsValidDepartmentCodeChar(code)))
        {
            throw new InvalidOperationException("部门编码只能包含英文、数字、连字符、下划线或点。");
        }
    }

    private static bool IsValidDepartmentCodeChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= 'A' and <= 'Z'
            || code is >= '0' and <= '9'
            || code is '-' or '_' or '.';
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<SysDepartment> GetDepartmentOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "部门主键必须大于 0。");
        }

        return await _departmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");
    }

    private async Task<SysDepartment?> ValidateParentAsync(long? parentId, long? currentDepartmentId, bool requireEnabledParent, CancellationToken cancellationToken)
    {
        if (!parentId.HasValue)
        {
            return null;
        }

        if (parentId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parentId), "父级部门主键必须大于 0。");
        }

        if (currentDepartmentId.HasValue && parentId.Value == currentDepartmentId.Value)
        {
            throw new InvalidOperationException("部门不能选择自身作为父级。");
        }

        var parent = await _departmentRepository.GetByIdAsync(parentId.Value, cancellationToken)
            ?? throw new InvalidOperationException("父级部门不存在。");

        if (requireEnabledParent && parent.Status != EnableStatus.Enabled)
        {
            throw new InvalidOperationException("启用部门不能挂载到停用父级部门下。");
        }

        await EnsureNoParentCycleAsync(parent, currentDepartmentId, cancellationToken);
        return parent;
    }

    private async Task EnsureNoParentCycleAsync(SysDepartment parent, long? currentDepartmentId, CancellationToken cancellationToken)
    {
        if (!currentDepartmentId.HasValue)
        {
            return;
        }

        var visited = new HashSet<long> { currentDepartmentId.Value };
        var cursor = parent;
        while (true)
        {
            if (!visited.Add(cursor.BasicId))
            {
                throw new InvalidOperationException("部门父子关系不能形成环路。");
            }

            if (!cursor.ParentId.HasValue)
            {
                return;
            }

            if (cursor.ParentId.Value == currentDepartmentId.Value)
            {
                throw new InvalidOperationException("部门父子关系不能形成环路。");
            }

            cursor = await _departmentRepository.GetByIdAsync(cursor.ParentId.Value, cancellationToken)
                ?? throw new InvalidOperationException("父级部门链路不完整。");
        }
    }

    private async Task ValidateLeaderAsync(long? leaderId, CancellationToken cancellationToken)
    {
        if (!leaderId.HasValue)
        {
            return;
        }

        if (leaderId.Value <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(leaderId), "负责人主键必须大于 0。");
        }

        var now = DateTimeOffset.UtcNow;
        var member = await _tenantUserRepository.GetMembershipAsync(leaderId.Value, cancellationToken)
            ?? throw new InvalidOperationException("负责人不是当前租户成员。");

        if (member.InviteStatus != TenantMemberInviteStatus.Accepted)
        {
            throw new InvalidOperationException("未接受邀请的租户成员不能作为部门负责人。");
        }

        if (member.Status != ValidityStatus.Valid)
        {
            throw new InvalidOperationException("无效租户成员不能作为部门负责人。");
        }

        if (member.EffectiveTime.HasValue && member.EffectiveTime.Value > now)
        {
            throw new InvalidOperationException("未生效租户成员不能作为部门负责人。");
        }

        if (member.ExpirationTime.HasValue && member.ExpirationTime.Value <= now)
        {
            throw new InvalidOperationException("已过期租户成员不能作为部门负责人。");
        }
    }

    private async Task EnsureCanDeleteDepartmentAsync(SysDepartment department, CancellationToken cancellationToken)
    {
        if (await _departmentRepository.HasChildrenAsync(department.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("部门存在子部门，不能直接删除。");
        }

        if (await _userDepartmentRepository.AnyAsync(link => link.DepartmentId == department.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("部门存在用户归属，不能删除。");
        }

        if (await _roleDataScopeRepository.AnyAsync(scope => scope.DepartmentId == department.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("部门存在角色数据范围引用，不能删除。");
        }

        if (await _userDataScopeRepository.AnyAsync(scope => scope.DepartmentId == department.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("部门存在用户数据范围引用，不能删除。");
        }

        if (await _fieldLevelSecurityRepository.AnyAsync(
            security => security.TargetType == FieldSecurityTargetType.Department && security.TargetId == department.BasicId,
            cancellationToken))
        {
            throw new InvalidOperationException("部门存在字段级安全策略引用，不能删除。");
        }
    }

    private async Task RebuildDepartmentHierarchyAsync(CancellationToken cancellationToken)
    {
        await _departmentHierarchyRepository.DeleteAsync(hierarchy => hierarchy.BasicId > 0, cancellationToken);

        var departments = await _departmentRepository.GetAllAsync(cancellationToken);
        if (departments.Count == 0)
        {
            return;
        }

        var rows = BuildHierarchyRows(departments);
        if (rows.Count > 0)
        {
            await _departmentHierarchyRepository.AddRangeAsync(rows, cancellationToken);
        }
    }
}
