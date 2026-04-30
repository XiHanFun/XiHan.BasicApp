#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DepartmentAppService
// Guid:c67b39a5-836c-4adc-bc20-d8bf5a26e82c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/30 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 部门命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "部门")]
public sealed class DepartmentAppService(
    IDepartmentRepository departmentRepository,
    IDepartmentHierarchyRepository departmentHierarchyRepository,
    ITenantUserRepository tenantUserRepository,
    IUserDepartmentRepository userDepartmentRepository,
    IRoleDataScopeRepository roleDataScopeRepository,
    IUserDataScopeRepository userDataScopeRepository,
    IFieldLevelSecurityRepository fieldLevelSecurityRepository)
    : SaasApplicationService, IDepartmentAppService
{
    /// <summary>
    /// 部门仓储
    /// </summary>
    private readonly IDepartmentRepository _departmentRepository = departmentRepository;

    /// <summary>
    /// 部门层级仓储
    /// </summary>
    private readonly IDepartmentHierarchyRepository _departmentHierarchyRepository = departmentHierarchyRepository;

    /// <summary>
    /// 租户成员仓储
    /// </summary>
    private readonly ITenantUserRepository _tenantUserRepository = tenantUserRepository;

    /// <summary>
    /// 用户部门仓储
    /// </summary>
    private readonly IUserDepartmentRepository _userDepartmentRepository = userDepartmentRepository;

    /// <summary>
    /// 角色数据范围仓储
    /// </summary>
    private readonly IRoleDataScopeRepository _roleDataScopeRepository = roleDataScopeRepository;

    /// <summary>
    /// 用户数据范围仓储
    /// </summary>
    private readonly IUserDataScopeRepository _userDataScopeRepository = userDataScopeRepository;

    /// <summary>
    /// 字段级安全仓储
    /// </summary>
    private readonly IFieldLevelSecurityRepository _fieldLevelSecurityRepository = fieldLevelSecurityRepository;

    /// <summary>
    /// 创建部门
    /// </summary>
    /// <param name="input">创建参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Department.Create)]
    public async Task<DepartmentDetailDto> CreateDepartmentAsync(DepartmentCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateCreateInput(input);

        var departmentCode = input.DepartmentCode.Trim();
        if (await _departmentRepository.GetByCodeAsync(departmentCode, cancellationToken) is not null)
        {
            throw new InvalidOperationException("部门编码已存在。");
        }

        _ = await ValidateParentAsync(input.ParentId, currentDepartmentId: null, input.Status == EnableStatus.Enabled, cancellationToken);
        await ValidateLeaderAsync(input.LeaderId, cancellationToken);

        var department = new SysDepartment
        {
            ParentId = input.ParentId,
            DepartmentName = input.DepartmentName.Trim(),
            DepartmentCode = departmentCode,
            DepartmentType = input.DepartmentType,
            LeaderId = input.LeaderId,
            Phone = NormalizeNullable(input.Phone),
            Email = NormalizeNullable(input.Email),
            Address = NormalizeNullable(input.Address),
            Status = input.Status,
            Sort = input.Sort,
            Remark = NormalizeNullable(input.Remark)
        };

        var savedDepartment = await _departmentRepository.AddAsync(department, cancellationToken);
        await RebuildDepartmentHierarchyAsync(cancellationToken);

        return DepartmentApplicationMapper.ToDetailDto(savedDepartment);
    }

    /// <summary>
    /// 更新部门
    /// </summary>
    /// <param name="input">更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Department.Update)]
    public async Task<DepartmentDetailDto> UpdateDepartmentAsync(DepartmentUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidateUpdateInput(input);

        var department = await GetDepartmentOrThrowAsync(input.BasicId, cancellationToken);
        var parentChanged = department.ParentId != input.ParentId;

        _ = await ValidateParentAsync(input.ParentId, department.BasicId, department.Status == EnableStatus.Enabled, cancellationToken);
        await ValidateLeaderAsync(input.LeaderId, cancellationToken);

        department.ParentId = input.ParentId;
        department.DepartmentName = input.DepartmentName.Trim();
        department.DepartmentType = input.DepartmentType;
        department.LeaderId = input.LeaderId;
        department.Phone = NormalizeNullable(input.Phone);
        department.Email = NormalizeNullable(input.Email);
        department.Address = NormalizeNullable(input.Address);
        department.Sort = input.Sort;
        department.Remark = NormalizeNullable(input.Remark);

        var savedDepartment = await _departmentRepository.UpdateAsync(department, cancellationToken);
        if (parentChanged)
        {
            await RebuildDepartmentHierarchyAsync(cancellationToken);
        }

        return DepartmentApplicationMapper.ToDetailDto(savedDepartment);
    }

    /// <summary>
    /// 更新部门状态
    /// </summary>
    /// <param name="input">状态更新参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门详情</returns>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Department.Status)]
    public async Task<DepartmentDetailDto> UpdateDepartmentStatusAsync(DepartmentStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "部门主键必须大于 0。");
        }

        ValidateEnum(input.Status, nameof(input.Status));

        var department = await GetDepartmentOrThrowAsync(input.BasicId, cancellationToken);
        if (input.Status == EnableStatus.Enabled)
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

        department.Status = input.Status;
        department.Remark = NormalizeNullable(input.Remark) ?? department.Remark;

        var savedDepartment = await _departmentRepository.UpdateAsync(department, cancellationToken);
        return DepartmentApplicationMapper.ToDetailDto(savedDepartment);
    }

    /// <summary>
    /// 删除部门
    /// </summary>
    /// <param name="id">部门主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Department.Delete)]
    public async Task DeleteDepartmentAsync(long id, CancellationToken cancellationToken = default)
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

    /// <summary>
    /// 获取部门，不存在时抛出异常
    /// </summary>
    /// <param name="id">部门主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>部门实体</returns>
    private async Task<SysDepartment> GetDepartmentOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "部门主键必须大于 0。");
        }

        return await _departmentRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("部门不存在。");
    }

    /// <summary>
    /// 校验父级部门
    /// </summary>
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

    /// <summary>
    /// 校验父级部门不存在环路
    /// </summary>
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

    /// <summary>
    /// 校验负责人是当前租户有效成员
    /// </summary>
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

    /// <summary>
    /// 校验部门可删除
    /// </summary>
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

    /// <summary>
    /// 重建当前租户部门闭包表
    /// </summary>
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

    /// <summary>
    /// 构建部门闭包表记录
    /// </summary>
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

    /// <summary>
    /// 构建当前部门到根部门的祖先链
    /// </summary>
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

    /// <summary>
    /// 校验创建参数
    /// </summary>
    private static void ValidateCreateInput(DepartmentCreateDto input)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(input.DepartmentCode);
        ValidateDepartmentCode(input.DepartmentCode);
        ValidateCommonInput(
            input.DepartmentName,
            input.DepartmentType,
            input.Phone,
            input.Email,
            input.Address,
            input.Remark);
        ValidateEnum(input.Status, nameof(input.Status));
    }

    /// <summary>
    /// 校验更新参数
    /// </summary>
    private static void ValidateUpdateInput(DepartmentUpdateDto input)
    {
        if (input.BasicId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(input), "部门主键必须大于 0。");
        }

        ValidateCommonInput(
            input.DepartmentName,
            input.DepartmentType,
            input.Phone,
            input.Email,
            input.Address,
            input.Remark);
    }

    /// <summary>
    /// 校验部门通用参数
    /// </summary>
    private static void ValidateCommonInput(
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

    /// <summary>
    /// 校验部门编码
    /// </summary>
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

    /// <summary>
    /// 判断部门编码字符是否合法
    /// </summary>
    private static bool IsValidDepartmentCodeChar(char code)
    {
        return code is >= 'a' and <= 'z'
            || code is >= 'A' and <= 'Z'
            || code is >= '0' and <= '9'
            || code is '-' or '_' or '.';
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验字符串长度
    /// </summary>
    private static void ValidateLength(string value, int maxLength, string paramName, string message)
    {
        if (value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 校验可空字符串长度
    /// </summary>
    private static void ValidateOptionalLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }

    /// <summary>
    /// 规范化可空字符串
    /// </summary>
    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
