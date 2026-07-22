// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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

    /// <summary>
    /// 表前缀（推断类名时剥离；逗号分隔多前缀）。默认剥离本系统常见前缀
    /// </summary>
    public string TablePrefixes { get; set; } = "Sys_,Saas_";

    /// <summary>
    /// 解析后的表前缀数组
    /// </summary>
    public IReadOnlyList<string> ResolvedTablePrefixes =>
        [.. TablePrefixes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
}
