// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 代码生成选项
/// </summary>
/// <remarks>
/// 绑定配置节 <see cref="SectionName"/>，只在开发环境配置（appsettings.Development.json）。
/// 「生成到项目」默认关闭：其他环境不配置即只能生成并下载，服务端不往磁盘写代码。
/// 写入位置不由表配置随意指定，而是按宿主内容根推导出本仓库的后端源码根与前端工程根。
/// </remarks>
public sealed class CodeGenerationOptions
{
    /// <summary>
    /// 配置节名
    /// </summary>
    public const string SectionName = "CodeGeneration";

    /// <summary>
    /// 是否允许生成到项目（默认关闭）
    /// </summary>
    public bool EnableGenerateToProject { get; set; }

    /// <summary>
    /// 后端源码根目录（相对宿主内容根或绝对路径）
    /// </summary>
    /// <remarks>
    /// 其下按分组目录（framework、modules、business 等）放项目，后端产物写进 <c>&lt;分组&gt;/&lt;命名空间&gt;/</c>，
    /// 以同名 csproj 认定项目。
    /// </remarks>
    public string? BackendRootPath { get; set; }

    /// <summary>
    /// 前端工程根目录（相对宿主内容根或绝对路径，须含 package.json）
    /// </summary>
    public string? FrontendRootPath { get; set; }

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
