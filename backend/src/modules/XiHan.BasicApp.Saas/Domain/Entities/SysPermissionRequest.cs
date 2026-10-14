// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 权限申请实体
/// 用户申请权限/角色 → 主管审批 → 自动授权的标准流程入口
/// </summary>
/// <remarks>
/// 职责边界：
/// - 本表承载"权限申请"业务语义，审批走工作流「权限申请」流程（业务编码 permission-request）
/// - 审批通过后由服务层自动写入 SysUserRole/SysUserPermission 完成授权
///
/// 关联：
/// - RequestUserId → SysUser（申请人）
/// - PermissionId → SysPermission（申请的权限，可空）
/// - RoleId → SysRole（申请的角色，可空）
/// - WorkflowInstanceId → 工作流实例（提交时发起流程后回填；实例的业务相关性标识即本表主键）
///
/// 写入：
/// - 申请时 RequestStatus=Pending + 发起审批流程
/// - 流程以 approved 结束 → RequestStatus=Approved + 自动授权
/// - 流程以 rejected 结束 → RequestStatus=Rejected
/// - 申请人撤回或流程被取消/终止 → RequestStatus=Withdrawn
///
/// 查询：
/// - 我的申请：IX_TeId_ReUsId + WHERE RequestUserId=?
/// - 按状态筛选：IX_TeId_ReSt
/// </remarks>
[SugarTable(TableName = "Sys_Permission_Request", TableDescription = "权限申请表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_ReUsId", nameof(TenantId), OrderByType.Asc, nameof(RequestUserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_ReSt", nameof(TenantId), OrderByType.Asc, nameof(RequestStatus), OrderByType.Asc)]
[SugarIndex("IX_{table}_WoInId", nameof(WorkflowInstanceId), OrderByType.Asc)]
public partial class SysPermissionRequest : BasicAppFullAuditedEntity, IStrictMultiTenantEntity
{
    /// <summary>
    /// 申请人ID
    /// </summary>
    [SugarColumn(ColumnName = "Request_User_Id", ColumnDescription = "申请人ID", IsNullable = false)]
    public virtual long RequestUserId { get; set; }

    /// <summary>
    /// 申请的权限ID（与 RoleId 二选一或同时填写）
    /// </summary>
    [SugarColumn(ColumnName = "Permission_Id", ColumnDescription = "权限ID", IsNullable = true)]
    public virtual long? PermissionId { get; set; }

    /// <summary>
    /// 申请的角色ID（与 PermissionId 二选一或同时填写）
    /// </summary>
    [SugarColumn(ColumnName = "Role_Id", ColumnDescription = "角色ID", IsNullable = true)]
    public virtual long? RoleId { get; set; }

    /// <summary>
    /// 申请原因
    /// </summary>
    [SugarColumn(ColumnName = "Request_Reason", ColumnDescription = "申请原因", Length = 1000, IsNullable = false)]
    public virtual string RequestReason { get; set; } = string.Empty;

    /// <summary>
    /// 期望生效时间（为空表示立即生效）
    /// </summary>
    [SugarColumn(ColumnName = "Expected_Effective_Time", ColumnDescription = "期望生效时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpectedEffectiveTime { get; set; }

    /// <summary>
    /// 期望失效时间（为空表示永久）
    /// </summary>
    [SugarColumn(ColumnName = "Expected_Expiration_Time", ColumnDescription = "期望失效时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpectedExpirationTime { get; set; }

    /// <summary>
    /// 审批流程实例ID（提交时发起流程后回填）
    /// </summary>
    [SugarColumn(ColumnName = "Workflow_Instance_Id", ColumnDescription = "审批流程实例ID", IsNullable = true)]
    public virtual long? WorkflowInstanceId { get; set; }

    /// <summary>
    /// 申请状态
    /// </summary>
    [SugarColumn(ColumnName = "Request_Status", ColumnDescription = "申请状态")]
    public virtual PermissionRequestStatus RequestStatus { get; set; } = PermissionRequestStatus.Pending;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnName = "Remark", ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
