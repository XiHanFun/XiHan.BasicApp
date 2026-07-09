#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:TraceQueryEnums
// Guid:5f3a1c02-7d64-4a8e-9b21-6c0e1f4a9d70
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/09 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 链路追踪维度
/// </summary>
/// <remarks>
/// 各维度在不同日志表的落列不同（会话/IP 列名各异），由 TraceQueryService 逐类型映射；
/// 部分日志不具备某维度（如权限变更日志无用户名/会话）时，该维度下不返回该类型条目。
/// </remarks>
public enum TraceDimension
{
    /// <summary>
    /// 用户名
    /// </summary>
    UserName = 0,

    /// <summary>
    /// 会话标识
    /// </summary>
    SessionId = 1,

    /// <summary>
    /// 链路追踪 ID
    /// </summary>
    TraceId = 2,

    /// <summary>
    /// IP 地址
    /// </summary>
    Ip = 3,

    /// <summary>
    /// 用户主键
    /// </summary>
    UserId = 4
}

/// <summary>
/// 链路追踪日志类型（既作请求范围，也作时间线条目类型）
/// </summary>
public enum TraceLogType
{
    /// <summary>
    /// 访问日志
    /// </summary>
    Access = 0,

    /// <summary>
    /// 开放接口日志
    /// </summary>
    Api = 1,

    /// <summary>
    /// 操作日志
    /// </summary>
    Operation = 2,

    /// <summary>
    /// 登录日志
    /// </summary>
    Login = 3,

    /// <summary>
    /// 异常日志
    /// </summary>
    Exception = 4,

    /// <summary>
    /// 数据变更日志
    /// </summary>
    Diff = 5,

    /// <summary>
    /// 权限变更日志
    /// </summary>
    PermissionChange = 6
}
