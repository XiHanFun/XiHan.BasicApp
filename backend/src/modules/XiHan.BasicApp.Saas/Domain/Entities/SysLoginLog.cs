#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysLoginLog
// Guid:4d28152c-d6e9-4396-addb-b479254bad24
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 03:55:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Core.Entities;
using XiHan.Framework.Domain.Entities.Abstracts;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 系统登录日志实体
/// 记录每次登录尝试（成功/失败）详细信息，用于风险检测与账户安全审计
/// </summary>
/// <remarks>
/// 分表策略：
/// - 按月分表，必带时间范围查询
///
/// 关联：
/// - UserId → SysUser（可能为空，失败登录时）；SessionId → SysUserSession（仅成功时）
/// - DeviceId 关联设备指纹库；TraceId 串联后续请求
///
/// 写入：
/// - 所有登录尝试（含失败）都必须记录，不得丢弃
/// - IsRiskLogin 由风控规则判定（异地/新设备/深夜登录等）
/// - LoginIp 必填，帮助异地登录告警
///
/// 查询：
/// - 用户登录历史：IX_UsId + 时间范围
/// - 异常用户扫描：IX_IsRi + WHERE IsRiskLogin=true
/// - 暴力破解检测：IX_LoIp / IX_UsNa + 窗口计数失败次数
/// - 租户登录趋势：IX_TeId_LoTi
///
/// 删除：
/// - 不支持业务删除；按合规保留期归档
///
/// 场景：
/// - 账户安全：异地/新设备登录实时通知
/// - 风控：短时间大量失败 → 临时封禁 IP
/// - 合规审计
/// </remarks>
[SugarTable("SysLoginLog_{year}{month}{day}", "系统登录日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_{split_table}_TeId_CrTi", nameof(TenantId), OrderByType.Asc, nameof(CreatedTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_CrId", nameof(CreatedId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_LoRe", nameof(LoginResult), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_UsNa", nameof(UserName), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_LoIp", nameof(LoginIp), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_TeId_LoTi", nameof(TenantId), OrderByType.Asc, nameof(LoginTime), OrderByType.Desc)]
[SugarIndex("IX_{split_table}_TrId", nameof(TraceId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_DeId", nameof(DeviceId), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_IsRi", nameof(IsRiskLogin), OrderByType.Asc)]
[SugarIndex("IX_{split_table}_SeId", nameof(SessionId), OrderByType.Asc)]
public partial class SysLoginLog : BasicAppCreationEntity, ISplitTableEntity, ITraceableEntity
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
    /// 登录IP
    /// </summary>
    [SugarColumn(ColumnDescription = "登录IP", Length = 50, IsNullable = true)]
    public virtual string? LoginIp { get; set; }

    /// <summary>
    /// 登录地址
    /// </summary>
    [SugarColumn(ColumnDescription = "登录地址", Length = 200, IsNullable = true)]
    public virtual string? LoginLocation { get; set; }

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
    /// 设备类型
    /// </summary>
    [SugarColumn(ColumnDescription = "设备类型", Length = 50, IsNullable = true)]
    public virtual string? Device { get; set; }

    /// <summary>
    /// 设备唯一标识，用于设备指纹和异地登录检测
    /// </summary>
    [SugarColumn(ColumnDescription = "设备唯一标识", Length = 200, IsNullable = true)]
    public virtual string? DeviceId { get; set; }

    /// <summary>
    /// 是否风险登录（异地登录、新设备登录等）
    /// </summary>
    [SugarColumn(ColumnDescription = "是否风险登录")]
    public virtual bool IsRiskLogin { get; set; } = false;

    /// <summary>
    /// 登录状态
    /// </summary>
    [SugarColumn(ColumnDescription = "登录状态")]
    public virtual LoginResult LoginResult { get; set; } = LoginResult.Success;

    /// <summary>
    /// 登录消息
    /// </summary>
    [SugarColumn(ColumnDescription = "登录消息", Length = 500, IsNullable = true)]
    public virtual string? Message { get; set; }

    /// <summary>
    /// 登录时间
    /// </summary>
    [SugarColumn(ColumnDescription = "登录时间")]
    public virtual DateTimeOffset LoginTime { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
