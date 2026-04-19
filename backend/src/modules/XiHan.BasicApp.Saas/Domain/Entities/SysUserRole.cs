#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysUserRole
// Guid:7c28152c-d6e9-4396-addb-b479254bad11
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 02:50:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统用户角色关联实体
/// 将角色（SysRole）赋予用户（SysUser），支持带生效期的临时授权
/// </summary>
/// <remarks>
/// 关联：
/// - UserId → SysUser；RoleId → SysRole
///
/// 写入：
/// - TenantId + UserId + RoleId 唯一（UX_TeId_UsId_RoId），避免重复授权（含 TenantId 以支持跨租户赋权场景）
/// - EffectiveTime 为空表示立即生效；ExpirationTime 为空表示永不过期
/// - 写入前必须校验：角色属于同租户（或为平台全局角色），违反则拒绝
/// - SoD 约束：写入前调用 SysConstraintRule/Item 检查职责分离（SSD/DSD）
///
/// 查询：
/// - 权限决策入口：按 UserId 查所有角色，再经 SysRoleHierarchy 展开继承链
/// - 角色反查用户：IX_RoId
/// - 即将过期扫描：按 ExTi 轮询清理（IX_ExTi）
///
/// 删除：
/// - 软删（基类 BasicAppFullAuditedEntity 提供 IsDeleted）；软删即撤销授权，保留审计记录
/// - 所有查询必须附加 IsDeleted=false 过滤
/// - 解绑时应同步撤销该用户在线会话中由此角色激活的 SysSessionRole
///
/// 状态：
/// - Status: Yes/No（停用 = 暂时冻结但保留关系）
///
/// 场景：
/// - 标准赋权：给用户分配角色
/// - 临时提权：EffectiveTime + ExpirationTime 限定时间窗口
/// - 多角色叠加：同一用户可持有多角色，权限取并集
/// </remarks>
[SugarTable("SysUserRole", "系统用户角色关联表")]
[SugarIndex("IX_{table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_IsDe", nameof(TenantId), OrderByType.Asc, nameof(IsDeleted), OrderByType.Asc)]
[SugarIndex("UX_{table}_TeId_UsId_RoId", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(RoleId), OrderByType.Asc, true)]
[SugarIndex("IX_{table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{table}_RoId", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_{table}_TeId_St", nameof(TenantId), OrderByType.Asc, nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{table}_EfTi", nameof(EffectiveTime), OrderByType.Asc)]
[SugarIndex("IX_{table}_ExTi", nameof(ExpirationTime), OrderByType.Asc)]
public partial class SysUserRole : BasicAppFullAuditedEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = false)]
    public virtual long UserId { get; set; }

    /// <summary>
    /// 角色ID
    /// </summary>
    [SugarColumn(ColumnDescription = "角色ID", IsNullable = false)]
    public virtual long RoleId { get; set; }

    /// <summary>
    /// 生效时间（为空表示立即生效）
    /// </summary>
    [SugarColumn(ColumnDescription = "生效时间", IsNullable = true)]
    public virtual DateTimeOffset? EffectiveTime { get; set; }

    /// <summary>
    /// 失效时间（为空表示永不过期）
    /// </summary>
    [SugarColumn(ColumnDescription = "失效时间", IsNullable = true)]
    public virtual DateTimeOffset? ExpirationTime { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    [SugarColumn(ColumnDescription = "状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }
}
