#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysPermissionRequest
// Guid:a7b8c9d0-e1f2-3456-2345-789012345678
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Entities.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限申请实体
/// 用户申请权限/角色 → 主管审批 → 自动授权的标准流程入口
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表承载"权限申请"业务语义，关联 SysReview 审批流
/// - 审批通过后由服务层自动写入 SysUserRole/SysUserPermission 完成授权
///
/// 关联：
/// - RequestUserId → SysUser（申请人）
/// - PermissionId → SysPermission（申请的权限，可空）
/// - RoleId → SysRole（申请的角色，可空）
/// - ReviewId → SysReview（关联的审批单，审批流创建后回填）
///
/// 写入：
/// - 申请时 RequestStatus=Pending + 创建 SysReview 审批单
/// - 审批通过 → RequestStatus=Approved + 自动授权
/// - 审批拒绝 → RequestStatus=Rejected
///
/// 查询：
/// - 我的申请：IX_TeId_ReUsId + WHERE RequestUserId=?
/// - 按状态筛选：IX_TeId_ReSt
/// </remarks>
[SugarTable("SysPermissionRequest", "权限申请表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_ReUsId", nameof(TenantId), OrderByType.Asc, nameof(RequestUserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_ReSt", nameof(TenantId), OrderByType.Asc, nameof(RequestStatus), OrderByType.Asc)]
[SugarIndex("IX_{table}_ReId", nameof(ReviewId), OrderByType.Asc)]
public partial class SysPermissionRequest : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 申请人ID
    /// </summary>
    [SugarColumn(ColumnDescription = "申请人ID", IsNullable = false)]
    public virtual long RequestUserId { get; set; }

    /// <summary>
    /// 申请的权限ID（与 RoleId 二选一或同时填写）
    /// </summary>
    [SugarColumn(ColumnDescription = "权限ID", IsNullable = true)]
    public virtual long? PermissionId { get; set; }

    /// <summary>
    /// 申请的角色ID（与 PermissionId 二选一或同时填写）
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = true)]
    public virtual long? RoleId { get; set; }

    /// <summary>
    /// 申请原因
    /// </summary>
    [SugarColumn(ColumnDescription = "申请原因", Length = 1000, IsNullable = false)]
    public virtual string RequestReason { get; set; } = string.Empty;

    /// <summary>
    /// 期望生效时间（为空表示立即生效）
    /// </summary>
    [SugarColumn(ColumnDescription = "期望生效时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpectedEffectiveTime { get; set; }

    /// <summary>
    /// 期望失效时间（为空表示永久）
    /// </summary>
    [SugarColumn(ColumnDescription = "期望失效时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpectedExpirationTime { get; set; }

    /// <summary>
    /// 关联审批单ID（审批流创建后回填）
    /// </summary>
    [SugarColumn(ColumnDescription = "审批单ID", IsNullable = true)]
    public virtual long? ReviewId { get; set; }

    /// <summary>
    /// 申请状态
    /// </summary>
    [SugarColumn(ColumnDescription = "申请状态")]
    public virtual PermissionRequestStatus RequestStatus { get; set; } = PermissionRequestStatus.Pending;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
