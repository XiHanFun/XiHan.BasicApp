#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysOperationLog
// Guid:7c81aea1-5298-46e8-b83b-7bcee87b103e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 05:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统操作日志实体
/// 业务操作轨迹（用户视角：点了什么/做了什么），对应前端操作行为
/// </summary>
/// <remarks>
/// 职责边界：
/// - 与 SysApiLog 区别：本表关注"业务语义"（用户导出了报表），SysApiLog 关注"HTTP 请求"（POST /api/export）
/// - 与 SysAuditLog 区别：本表覆盖更宽泛的操作，SysAuditLog 仅针对敏感/合规级操作
///
/// 分表策略：
/// - 按月分表；查询/清理必带时间范围
///
/// 关联：
/// - UserId → SysUser；SessionId → SysUserSession；TraceId 串联请求链
///
/// 写入：
/// - 由业务切面（AOP）/拦截器统一写入
/// - OperationType 用于快速分类（查询/新增/修改/删除/导出等）
/// - Status 记录该操作最终结果（成功/失败/部分成功）
///
/// 查询：
/// - 用户操作历史（个人中心"我的操作"）：IX_UsId + 时间范围
/// - 租户操作趋势：IX_TeId_OpTi
/// - 按类型聚合：IX_OpTy
/// - 失败操作分析：IX_St
///
/// 删除：
/// - 不支持业务删除；按保留策略清理
///
/// 场景：
/// - 用户自助查看操作历史
/// - 管理员审查可疑行为
/// - 行为分析驱动产品优化
/// </remarks>
[SugarTable("SysOperationLog_{year}{month}{day}", "系统操作日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_OpTy", nameof(OperationType), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TeId_OpTi", nameof(TenantId), OrderByType.Asc, nameof(OperationTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_St", nameof(Status), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TrId", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_SeId", nameof(SessionId), OrderByType.Asc)]
public partial class SysOperationLog : BasicAppCreationEntity, ISplitTableEntity, ITraceableEntity
{
    /// <summary>
    /// 用户ID
    /// </summary>
    [SugarColumn(ColumnDescription = "用户ID", IsNullable = true)]
    public virtual long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    [SugarColumn(ColumnDescription = "用户名", Length = 50, IsNullable = true)]
    public virtual string? UserName { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "会话ID", Length = 100, IsNullable = true)]
    public virtual string? SessionId { get; set; }

    /// <summary>
    /// 链路追踪ID，用于串联整个请求生命周期
    /// </summary>
    [SugarColumn(ColumnDescription = "链路追踪ID", Length = 64, IsNullable = true)]
    public virtual string? TraceId { get; set; }

    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(ColumnDescription = "操作类型")]
    public virtual OperationType OperationType { get; set; } = OperationType.Other;

    /// <summary>
    /// 操作模块
    /// </summary>
    [SugarColumn(ColumnDescription = "操作模块", Length = 50, IsNullable = true)]
    public virtual string? Module { get; set; }

    /// <summary>
    /// 操作功能
    /// </summary>
    [SugarColumn(ColumnDescription = "操作功能", Length = 50, IsNullable = true)]
    public virtual string? Function { get; set; }

    /// <summary>
    /// 操作标题
    /// </summary>
    [SugarColumn(ColumnDescription = "操作标题", Length = 200, IsNullable = true)]
    public virtual string? Title { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    [SugarColumn(ColumnDescription = "操作描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnDescription = "请求方法", Length = 10, IsNullable = true)]
    public virtual string? Method { get; set; }

    /// <summary>
    /// 请求URL
    /// </summary>
    [SugarColumn(ColumnDescription = "请求URL", Length = 500, IsNullable = true)]
    public virtual string? RequestUrl { get; set; }

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "执行时间（毫秒）")]
    public virtual long ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 操作IP
    /// </summary>
    [SugarColumn(ColumnDescription = "操作IP", Length = 50, IsNullable = true)]
    public virtual string? OperationIp { get; set; }

    /// <summary>
    /// 操作地址
    /// </summary>
    [SugarColumn(ColumnDescription = "操作地址", Length = 200, IsNullable = true)]
    public virtual string? OperationLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    [SugarColumn(ColumnDescription = "浏览器类型", Length = 100, IsNullable = true)]
    public virtual string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    [SugarColumn(ColumnDescription = "操作系统", Length = 100, IsNullable = true)]
    public virtual string? Os { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    [SugarColumn(ColumnDescription = "User-Agent", Length = 500, IsNullable = true)]
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// 操作状态
    /// </summary>
    [SugarColumn(ColumnDescription = "操作状态")]
    public virtual YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 错误消息
    /// </summary>
    [SugarColumn(ColumnDescription = "错误消息", Length = 1000, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [SugarColumn(ColumnDescription = "操作时间")]
    public virtual DateTimeOffset OperationTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
