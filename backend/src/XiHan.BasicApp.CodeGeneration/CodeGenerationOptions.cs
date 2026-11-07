// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Configuration;

namespace XiHan.BasicApp.CodeGeneration;

/// <summary>
/// 代码生成的选项
/// </summary>
/// <remarks>
/// 配置节 <c>BasicApp:CodeGeneration</c>（4.x 的 <c>CodeGeneration</c>）：写进项目的开关与前后端根目录、导入时去掉的表名前缀
/// 写进项目只在 Development 且开关打开时可用（方案 C1）
/// </remarks>
public sealed class CodeGenerationOptions
{
    /// <summary>
    /// 配置节
    /// </summary>
    public const string Section = "BasicApp:CodeGeneration";

    /// <summary>
    /// 是否允许写进项目
    /// </summary>
    /// <remarks>
    /// 缺省关；打开了也只在 Development 生效
    /// </remarks>
    public bool EnableGenerateToProject { get; init; }

    /// <summary>
    /// 是否开放 SQLite 数据源
    /// </summary>
    /// <remarks>
    /// 缺省关：SQLite 的「地址」是服务器上的文件路径，开放等于能读进程够得着的任意数据库文件
    /// </remarks>
    public bool AllowSqliteDataSource { get; init; }

    /// <summary>
    /// 后端根目录
    /// </summary>
    /// <remarks>
    /// 含 <c>src/</c> 的 <c>backend-next</c> 目录，相对宿主的内容根目录
    /// </remarks>
    public string? BackendRootPath { get; init; }

    /// <summary>
    /// 前端根目录
    /// </summary>
    /// <remarks>
    /// 含 <c>package.json</c> 的 <c>frontend-next</c> 目录，相对宿主的内容根目录
    /// </remarks>
    public string? FrontendRootPath { get; init; }

    /// <summary>
    /// 导入时去掉的表名前缀
    /// </summary>
    /// <remarks>
    /// 逗号分隔，缺省 <c>sys_</c>（6.0 的表名是 snake_case；4.x 缺省 <c>Sys_,Saas_</c>）。推断类名时去掉最长的匹配前缀
    /// </remarks>
    public IReadOnlyList<string> TablePrefixes { get; init; } = ["sys_"];

    /// <summary>
    /// 从配置读取
    /// </summary>
    /// <param name="configuration">配置</param>
    public static CodeGenerationOptions FromConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var section = configuration.GetSection(Section);
        var enabled = section[nameof(EnableGenerateToProject)];
        var sqlite = section[nameof(AllowSqliteDataSource)];
        return new CodeGenerationOptions
        {
            AllowSqliteDataSource = sqlite is not null && (bool.TryParse(sqlite, out var allowed) ? allowed : throw new ArgumentException($"'{Section}:{nameof(AllowSqliteDataSource)}' must be 'true' or 'false', but was '{sqlite}'.")),
            EnableGenerateToProject = enabled is not null && (bool.TryParse(enabled, out var value) ? value : throw new ArgumentException($"'{Section}:{nameof(EnableGenerateToProject)}' must be 'true' or 'false', but was '{enabled}'.")),
            BackendRootPath = section[nameof(BackendRootPath)] is { Length: > 0 } backend ? backend : null,
            FrontendRootPath = section[nameof(FrontendRootPath)] is { Length: > 0 } frontend ? frontend : null,
            TablePrefixes = section[nameof(TablePrefixes)] is { } prefixes
                ? [.. prefixes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)]
                : ["sys_"],
        };
    }
}
