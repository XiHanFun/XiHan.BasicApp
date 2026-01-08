#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SysAuditLogDto
// Guid:2628152c-d6e9-4396-addb-b479254bad98
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/1/8 0:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Dtos.Base;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.Dtos;

/// <summary>
/// 系统审计日志查询 DTO
/// </summary>
public class SysAuditLogGetDto : RbacFullAuditedDtoBase
{
    /// <summary>
    /// 租户ID
    /// </summary>
    public long? TenantId { get; set; }

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// 用户真实姓名
    /// </summary>
    public string? RealName { get; set; }

    /// <summary>
    /// 部门ID
    /// </summary>
    public long? DepartmentId { get; set; }

    /// <summary>
    /// 部门名称
    /// </summary>
    public string? DepartmentName { get; set; }

    /// <summary>
    /// 审计类型
    /// </summary>
    public string AuditType { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    public OperationType OperationType { get; set; }

    /// <summary>
    /// 实体类型
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// 实体ID
    /// </summary>
    public string? EntityId { get; set; }

    /// <summary>
    /// 实体名称
    /// </summary>
    public string? EntityName { get; set; }

    /// <summary>
    /// 表名称
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    /// 主键字段
    /// </summary>
    public string? PrimaryKey { get; set; }

    /// <summary>
    /// 主键值
    /// </summary>
    public string? PrimaryKeyValue { get; set; }

    /// <summary>
    /// 操作模块
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// 操作功能
    /// </summary>
    public string? Function { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 操作前数据
    /// </summary>
    public string? BeforeData { get; set; }

    /// <summary>
    /// 操作后数据
    /// </summary>
    public string? AfterData { get; set; }

    /// <summary>
    /// 变更字段
    /// </summary>
    public string? ChangedFields { get; set; }

    /// <summary>
    /// 变更内容描述
    /// </summary>
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// 请求路径
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// 请求方法
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// 请求参数
    /// </summary>
    public string? RequestParams { get; set; }

    /// <summary>
    /// 响应结果
    /// </summary>
    public string? ResponseResult { get; set; }

    /// <summary>
    /// 执行时间（毫秒）
    /// </summary>
    public long ExecutionTime { get; set; } = 0;

    /// <summary>
    /// 操作IP
    /// </summary>
    public string? OperationIp { get; set; }

    /// <summary>
    /// 操作地址
    /// </summary>
    public string? OperationLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 设备类型
    /// </summary>
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;

    /// <summary>
    /// 设备信息
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// User-Agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 会话ID
    /// </summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// 请求ID
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// 关联业务ID
    /// </summary>
    public string? BusinessId { get; set; }

    /// <summary>
    /// 关联业务类型
    /// </summary>
    public string? BusinessType { get; set; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool IsSuccess { get; set; } = true;

    /// <summary>
    /// 异常信息
    /// </summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>
    /// 异常堆栈
    /// </summary>
    public string? ExceptionStackTrace { get; set; }

    /// <summary>
    /// 风险等级（1-5，数字越大风险越高）
    /// </summary>
    public int RiskLevel { get; set; } = 1;

    /// <summary>
    /// 审计时间
    /// </summary>
    public DateTimeOffset AuditTime { get; set; } = DateTimeOffset.Now;

    /// <summary>
    /// 扩展数据
    /// </summary>
    public string? ExtendData { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}
