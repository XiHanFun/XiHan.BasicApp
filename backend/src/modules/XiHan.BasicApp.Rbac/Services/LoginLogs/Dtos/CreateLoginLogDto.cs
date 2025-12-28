#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CreateLoginLogDto
// Guid:h2c2d3e4-f5a6-7890-abcd-ef1234567907
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/12/28 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core;
using XiHan.BasicApp.Rbac.Enums;

namespace XiHan.BasicApp.Rbac.Services.LoginLogs.Dtos;

/// <summary>
/// 创建登录日志 DTO
/// </summary>
public class CreateLoginLogDto
{
    /// <summary>
    /// 用户ID
    /// </summary>
    public XiHanBasicAppIdType UserId { get; set; }

    /// <summary>
    /// 用户名
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// 登录IP
    /// </summary>
    public string? LoginIp { get; set; }

    /// <summary>
    /// 登录地址
    /// </summary>
    public string? LoginLocation { get; set; }

    /// <summary>
    /// 浏览器类型
    /// </summary>
    public string? Browser { get; set; }

    /// <summary>
    /// 操作系统
    /// </summary>
    public string? Os { get; set; }

    /// <summary>
    /// 登录状态
    /// </summary>
    public YesOrNo Status { get; set; } = YesOrNo.Yes;

    /// <summary>
    /// 登录消息
    /// </summary>
    public string? Message { get; set; }
}
