#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DemoEnvironmentOptions
// Guid:a1b2c3d4-5e6f-7a8b-9c0d-1e2f3a4b5c6d
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/30 19:37:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Web.Core.Options;

/// <summary>
/// 演示环境配置项
/// </summary>
public class DemoEnvironmentOptions
{
    /// <summary>
    /// 配置节名称
    /// </summary>
    public const string SectionName = "XiHan:Demo";

    /// <summary>
    /// 是否启用演示环境
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// 演示环境禁止修改数据的提示消息
    /// </summary>
    public string Message { get; set; } = "演示环境禁止修改数据，仅供查看";

    /// <summary>
    /// 允许在演示环境中执行的路径（白名单）
    /// </summary>
    public List<string> AllowedPaths { get; set; } = [];
}
