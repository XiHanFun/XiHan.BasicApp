#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenerationOptions
// Guid:c0de9e00-0401-4a00-9000-000000000401
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/04 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 代码生成选项（受控落盘等安全策略）
/// </summary>
/// <remarks>
/// 绑定配置节 <see cref="SectionName"/>；默认禁用自定义路径落盘，
/// 需显式开启并配置白名单根目录，配合路径穿越校验 fail-closed。
/// </remarks>
public sealed class CodeGenerationOptions
{
    /// <summary>
    /// 配置节名
    /// </summary>
    public const string SectionName = "CodeGeneration";

    /// <summary>
    /// 是否启用自定义路径落盘（默认禁用；生产须显式开启）
    /// </summary>
    public bool EnableCustomPathDisk { get; set; }

    /// <summary>
    /// 允许落盘的根目录白名单（绝对路径；为空则落盘被拒绝）
    /// </summary>
    public IReadOnlyList<string> AllowedRootPaths { get; set; } = [];
}
