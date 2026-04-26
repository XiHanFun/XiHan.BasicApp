#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysTenantEditionPermission
// Guid:d4e5f6a7-b8c9-0123-def0-234567890104
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Entities.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 租户版本可用权限映射实体
/// Plan-gating（订阅门控）核心：定义每个订阅版本(Edition)向租户开放的权限白名单
/// </summary>
/// <remarks>
/// 关联：
/// - EditionId → SysTenantEdition；PermissionId → SysPermission（通常是 IsGlobal=true 的全局权限）
///
/// 写入：
/// - EditionId + PermissionId 唯一（UX_EdId_PeId）
/// - 写入前必须校验：PermissionId 对应的权限 IsGlobal=true（非全局权限不应被版本门控）
/// - Edition 升级（如 Basic → Pro）时应增量写入新增权限
///
/// 查询：
/// - 租户可用权限集合计算：SysTenant.EditionId → 此表 → 可用 PermissionId 集
/// - 租户管理员分配角色权限时，必须在此集合内选择
///
/// 删除：
/// - 硬删；删除后对应权限立即对该 Edition 下所有租户不可用（服务层需刷新缓存）
/// - 降级场景：先检查租户已分配的角色/用户权限是否包含被移除项，提示清理
///
/// 状态：
/// - Status: Yes/No（停用单条映射，不影响 Edition 主记录）
///
/// 场景：
/// - Free 版：基础 CRUD 权限
/// - Pro 版：+ 高级报表、批量导出、审批流
/// - Enterprise：+ SSO、审计报告、自定义字段等全部权限
/// </remarks>
[SugarTable("SysTenantEditionPermission", "租户版本可用权限映射表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("UX_{table}_EdId_PeId", nameof(EditionId), OrderByType.Asc, nameof(PermissionId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_EdId", nameof(EditionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_PeId", nameof(PermissionId), OrderByType.Asc)]
[SugarIndex("IX_{table}_St", nameof(Status), OrderByType.Asc)]
public partial class SysTenantEditionPermission : BasicAppCreationEntity
{
    /// <summary>
    /// 版本ID
    /// </summary>
    [SugarColumn(ColumnDescription = "版本ID", IsNullable = false)]
    public virtual long EditionId { get; set; }

    /// <summary>
    /// 权限ID（关联平台级全局权限）
    /// </summary>
    [SugarColumn(ColumnDescription = "权限ID", IsNullable = false)]
    public virtual long PermissionId { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual ValidityStatus Status { get; set; } = ValidityStatus.Valid;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
