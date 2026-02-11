#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysApiLog
// Guid:3d28152c-d6e9-4396-addb-b479254bad53
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/08/14 06:40:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using SqlSugar;
using XiHan.BasicApp.Rbac.Entities.Base;

namespace XiHan.BasicApp.Rbac.Entities;

/// <summary>
/// 系统接口日志实体
/// </summary>
[SugarTable("Sys_Api_Log_{year}{month}{day}", "系统接口日志表"), SplitTable(SplitType.Month)]
[SugarIndex("IX_SysApiLog_UserId", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_SysApiLog_ApiPath", nameof(ApiPath), OrderByType.Asc)]
[SugarIndex("IX_SysApiLog_Method", nameof(Method), OrderByType.Asc)]
[SugarIndex("IX_SysApiLog_StatusCode", nameof(StatusCode), OrderByType.Asc)]
[SugarIndex("IX_SysApiLog_RequestTime", nameof(RequestTime), OrderByType.Desc)]
[SugarIndex("IX_SysApiLog_TenantId", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_SysApiLog_TenantId_RequestTime", nameof(TenantId), OrderByType.Asc, nameof(RequestTime), OrderByType.Desc)]
[SugarIndex("IX_SysApiLog_ExecutionTime", nameof(ExecutionTime), OrderByType.Desc)]
public partial class SysApiLog : RbacCreationEntity<long>
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
    /// 请求ID
    /// </summary>
    [SugarColumn(ColumnDescription = "请求ID", Length = 100, IsNullable = true)]
    public virtual string? RequestId { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    [SugarColumn(ColumnDescription = "会话ID", Length = 100, IsNullable = true)]
    public virtual string? SessionId { get; set; }

    /// <summary>
    /// API路径
    /// </summary>
    [SugarColumn(ColumnDescription = "API路径", Length = 500, IsNullable = false)]
    public virtual string ApiPath { get; set; } = string.Empty;

    /// <summary>
    /// API名称
    /// </summary>
    [SugarColumn(ColumnDescription = "API名称", Length = 200, IsNullable = true)]
    public virtual string? ApiName { get; set; }

    /// <summary>
    /// API描述
    /// </summary>
    [SugarColumn(ColumnDescription = "API描述", Length = 500, IsNullable = true)]
    public virtual string? ApiDescription { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    [SugarColumn(ColumnDescription = "请求方法", Length = 10, IsNullable = false)]
    public virtual string Method { get; set; } = string.Empty;

    /// <summary>
    /// 控制器名称
    /// </summary>
    [SugarColumn(ColumnDescription = "控制器名称", Length = 100, IsNullable = true)]
    public virtual string? ControllerName { get; set; }

    /// <summary>
    /// 操作名称
    /// </summary>
    [SugarColumn(ColumnDescription = "操作名称", Length = 100, IsNullable = true)]
    public virtual string? ActionName { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    [SugarColumn(ColumnDescription = "请求参数", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestParams { get; set; }

    /// <summary>
    /// 请求体
    /// </summary>
    [SugarColumn(ColumnDescription = "请求体", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestBody { get; set; }

    /// <summary>
    /// 响应结果
    /// </summary>
    [SugarColumn(ColumnDescription = "响应结果", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ResponseBody { get; set; }

    /// <summary>
    /// 响应状态码
    /// </summary>
    [SugarColumn(ColumnDescription = "响应状态码")]
    public virtual int StatusCode { get; set; } = 200;

    /// <summary>
    /// 请求头
    /// </summary>
    [SugarColumn(ColumnDescription = "请求头", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? RequestHeaders { get; set; }

    /// <summary>
    /// 响应头
    /// </summary>
    [SugarColumn(ColumnDescription = "响应头", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ResponseHeaders { get; set; }

    /// <summary>
    /// 请求IP
    /// </summary>
    [SugarColumn(ColumnDescription = "请求IP", Length = 50, IsNullable = true)]
    public virtual string? RequestIp { get; set; }

    /// <summary>
    /// 请求地址
    /// </summary>
    [SugarColumn(ColumnDescription = "请求地址", Length = 200, IsNullable = true)]
    public virtual string? RequestLocation { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    [SugarColumn(ColumnDescription = "User-Agent", Length = 500, IsNullable = true)]
    public virtual string? UserAgent { get; set; }

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
    /// 请求来源
    /// </summary>
    [SugarColumn(ColumnDescription = "请求来源", Length = 500, IsNullable = true)]
    public virtual string? Referer { get; set; }

    /// <summary>
    /// 请求时间
    /// </summary>
    [SugarColumn(ColumnDescription = "请求时间")]
    public virtual DateTimeOffset RequestTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 响应时间
    /// </summary>
    [SugarColumn(ColumnDescription = "响应时间", IsNullable = true)]
    public virtual DateTimeOffset? ResponseTime { get; set; }

    /// <summary>
    /// 执行时长（毫秒）
    /// </summary>
    [SugarColumn(ColumnDescription = "执行时长（毫秒）")]
    public virtual long ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 请求大小（字节）
    /// </summary>
    [SugarColumn(ColumnDescription = "请求大小（字节）")]
    public virtual long RequestSize { get; set; } = 0;

    /// <summary>
    /// 响应大小（字节）
    /// </summary>
    [SugarColumn(ColumnDescription = "响应大小（字节）")]
    public virtual long ResponseSize { get; set; } = 0;

    /// <summary>
    /// 是否成功
    /// </summary>
    [SugarColumn(ColumnDescription = "是否成功")]
    public virtual bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 错误信息
    /// </summary>
    [SugarColumn(ColumnDescription = "错误信息", Length = 2000, IsNullable = true)]
    public virtual string? ErrorMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    [SugarColumn(ColumnDescription = "异常堆栈", ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public virtual string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// API版本
    /// </summary>
    [SugarColumn(ColumnDescription = "API版本", Length = 20, IsNullable = true)]
    public virtual string? ApiVersion { get; set; }

    /// <summary>
    /// 业务类型
    /// </summary>
    [SugarColumn(ColumnDescription = "业务类型", Length = 50, IsNullable = true)]
    public virtual string? BusinessType { get; set; }

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
