#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLog
// Guid:c528152c-d6e9-4396-addb-b479254bad62
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/01/08 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统审计日志实体
/// </summary>
[SugarTable("Sys_Audit_Log_{year}{month}{day}", "系统审计日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_SysAuditLog_UsId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysAuditLog_AuTy", nameof(AuditType), OrderByType.Asc)]
[SugarIndex("IX_SysAuditLog_EnTy", nameof(EntityType), OrderByType.Asc)]
[SugarIndex("IX_SysAuditLog_OpTy", nameof(OperationType), OrderByType.Asc)]
[SugarIndex("IX_SysAuditLog_AuTi", nameof(AuditTime), OrderByType.Desc)]
[SugarIndex("IX_SysAuditLog_TeId_AuTi", nameof(TenantId), OrderByType.Asc, nameof(AuditTime), OrderByType.Desc)]
[SugarIndex("IX_SysAuditLog_RiLe", nameof(RiskLevel), OrderByType.Desc)]
[SugarIndex("IX_SysAuditLog_EnId", nameof(EntityId), OrderByType.Asc)]
public partial class SysAuditLog : RbacCreationEntity<long>
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
    /// 用户真实姓名
    /// </summary>
    [SugarColumn(ColumnDescription = "用户真实姓名", Length = 50, IsNullable = true)]
    public virtual string? RealName { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    [SugarColumn(ColumnDescription = "部门ID", IsNullable = true)]
    public virtual long? DepartmentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    [SugarColumn(ColumnDescription = "部门名称", Length = 100, IsNullable = true)]
    public virtual string? DepartmentName { get; set; }

    /// <summary>
    /// 审计类型
    /// </summary>
    [SugarColumn(ColumnDescription = "审计类型", Length = 50, IsNullable = false)]
    public virtual string AuditType { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(ColumnDescription = "操作类型")]
    public virtual OperationType OperationType { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    [SugarColumn(ColumnDescription = "实体类型", Length = 100, IsNullable = true)]
    public virtual string? EntityType { get; set; }

    /// <summary>
    /// 实体ID
    /// </summary>
    [SugarColumn(ColumnDescription = "实体ID", Length = 100, IsNullable = true)]
    public virtual string? EntityId { get; set; }

    /// <summary>
    /// 实体名称
    /// </summary>
    [SugarColumn(ColumnDescription = "实体名称", Length = 200, IsNullable = true)]
    public virtual string? EntityName { get; set; }

    /// <summary>
    /// 表名称
    /// </summary>
    [SugarColumn(ColumnDescription = "表名称", Length = 100, IsNullable = true)]
    public virtual string? TableName { get; set; }

    /// <summary>
    /// 主键字段
    /// </summary>
    [SugarColumn(ColumnDescription = "主键字段", Length = 50, IsNullable = true)]
    public virtual string? PrimaryKey { get; set; }

    /// <summary>
    /// 主键值
    /// </summary>
    [SugarColumn(ColumnDescription = "主键值", Length = 100, IsNullable = true)]
    public virtual string? PrimaryKeyValue { get; set; }

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
    /// 操作描述
    /// </summary>
    [SugarColumn(ColumnDescription = "操作描述", Length = 500, IsNullable = true)]
    public virtual string? Description { get; set; }

    /// <summary>
    /// 操作前数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "操作前数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? BeforeData { get; set; }

    /// <summary>
    /// 操作后数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "操作后数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? AfterData { get; set; }

    /// <summary>
    /// 变更字段（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "变更字段", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ChangedFields { get; set; }

    /// <summary>
    /// 变更内容描述
    /// </summary>
    [SugarColumn(ColumnDescription = "变更内容描述", Length = 1000, IsNullable = true)]
    public virtual string? ChangeDescription { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    [SugarColumn(ColumnDescription = "请求路径", Length = 500, IsNullable = true)]
    public virtual string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnDescription = "请求方法", Length = 10, IsNullable = true)]
    public virtual string? RequestMethod { get; set; }

    /// <summary>
    /// 请求参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "请求参数", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestParams { get; set; }

    /// <summary>
    /// 响应结果（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "响应结果", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ResponseResult { get; set; }

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
    /// 设备类型
    /// </summary>
    [SugarColumn(ColumnDescription = "设备类型")]
    public virtual DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备信息
    /// </summary>
    [SugarColumn(ColumnDescription = "设备信息", Length = 200, IsNullable = true)]
    public virtual string? DeviceInfo { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    [SugarColumn(ColumnDescription = "User-Agent", Length = 500, IsNullable = true)]
    public virtual string? UserAgent { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "会话ID", Length = 100, IsNullable = true)]
    public virtual string? SessionId { get; set; }

    /// <summary>
    /// 请求ID
    /// </summary>
    [SugarColumn(ColumnDescription = "请求ID", Length = 100, IsNullable = true)]
    public virtual string? RequestId { get; set; }

    /// <summary>
    /// 关联业务ID
    /// </summary>
    [SugarColumn(ColumnDescription = "关联业务ID", Length = 100, IsNullable = true)]
    public virtual string? BusinessId { get; set; }

    /// <summary>
    /// 关联业务类型
    /// </summary>
    [SugarColumn(ColumnDescription = "关联业务类型", Length = 50, IsNullable = true)]
    public virtual string? BusinessType { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnDescription = "是否成功")]
    public virtual bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 异常信息
    /// </summary>
    [SugarColumn(ColumnDescription = "异常信息", Length = 2000, IsNullable = true)]
    public virtual string? ExceptionMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    [SugarColumn(ColumnDescription = "异常堆栈", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 风险等级（1-5，数字越大风险越高）
    /// </summary>
    [SugarColumn(ColumnDescription = "风险等级")]
    public virtual int RiskLevel { get; set; } = 1;

    /// <summary>
    /// 审计时间
    /// </summary>
    [SugarColumn(ColumnDescription = "审计时间")]
    public virtual DateTimeOffset AuditTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 扩展数据（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDescription = "扩展数据", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(ColumnDescription = "备注", Length = 500, IsNullable = true)]
    public virtual string? Remark { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    [SugarColumn(IsNullable = false, ColumnDescription = "创建时间")]
    [SplitField]
    public override DateTimeOffset CreatedTime { get; set; }
}
