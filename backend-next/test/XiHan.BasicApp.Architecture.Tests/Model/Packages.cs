// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Xml.Linq;
using XiHan.BasicApp.Access;
using XiHan.BasicApp.Auditing;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Identity;
using XiHan.BasicApp.Runtime;
using XiHan.BasicApp.Organization;
using XiHan.BasicApp.Settings;
using XiHan.BasicApp.Tenancy;
using XiHan.BasicApp.Workbench;

namespace XiHan.BasicApp.Architecture.Tests.Model;

/// <summary>
/// 方案 D2 的包目录与层级：只能向下依赖、同层之间不引用。秩相同即同层（Tier 2 与 Tier 3 内部互不引用），Tier 0 / Tier 1 的链按箭头逐个递增。
/// </summary>
public static class Packages
{
    public const string Prefix = "XiHan.BasicApp.";

    /// <summary>
    /// 包名（去前缀）→ 秩；引用只能指向秩更小的包。
    /// </summary>
    public static IReadOnlyDictionary<string, int> Ranks { get; } = new Dictionary<string, int>(StringComparer.Ordinal)
    {
        // Tier 0：Contracts → Runtime
        ["Contracts"] = 0,
        ["Runtime"] = 1,
        // Tier 1：Identity → Tenancy → Organization → Access
        ["Identity"] = 10,
        ["Tenancy"] = 11,
        ["Organization"] = 12,
        ["Access"] = 13,
        // Tier 2：只依赖 Tier 0 / 1，彼此不引用
        ["Settings"] = 20,
        ["Notification"] = 20,
        ["Files"] = 20,
        ["Auditing"] = 20,
        ["Maintenance"] = 20,
        ["OpenPlatform"] = 20,
        ["Realtime"] = 20,
        // Tier 3：可选模块，彼此不引用
        ["Workbench"] = 30,
        ["Workflow"] = 30,
        ["Printing"] = 30,
        ["CodeGeneration"] = 30,
        ["Chat"] = 30,
        ["AI"] = 30,
        // 宿主
        ["Host"] = 100,
    };

    /// <summary>
    /// Tier 0 基座与宿主不按能力包的目录结构约束。
    /// </summary>
    public static IReadOnlySet<string> StructureExempt { get; } = new HashSet<string>(StringComparer.Ordinal) { "Contracts", "Runtime", "Host" };

    /// <summary>
    /// 能力包顶层目录白名单（方案 5.3）。
    /// </summary>
    public static IReadOnlySet<string> CapabilityDirectories { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "Domain", "Operations", "Queries", "Handlers", "Middleware", "Jobs", "Infrastructure", "Seeds", "Schema", "Localization",
    };

    /// <summary>
    /// 已装进测试进程的正式程序集（宿主引用了全部能力包）。
    /// </summary>
    public static IReadOnlyList<Assembly> Assemblies { get; } =
    [
        typeof(AppInfo).Assembly,
        typeof(RuntimeFeature).Assembly,
        typeof(IdentityFeature).Assembly,
        typeof(TenancyFeature).Assembly,
        typeof(OrganizationFeature).Assembly,
        typeof(AccessFeature).Assembly,
        typeof(SettingsFeature).Assembly,
        typeof(AuditingFeature).Assembly,
        typeof(WorkbenchFeature).Assembly,
        typeof(BasicAppHost).Assembly,
    ];

    /// <summary>
    /// backend-next 目录（含 XiHan.BasicApp.slnx）。
    /// </summary>
    public static string Root { get; } = Locate();

    public static string SourceRoot => Path.Combine(Root, "src");

    /// <summary>
    /// src 下全部工程：包名（去前缀）→ 工程文件路径。
    /// </summary>
    public static IReadOnlyDictionary<string, string> Projects { get; } = Directory
        .EnumerateFiles(SourceRoot, "*.csproj", SearchOption.AllDirectories)
        .ToDictionary(static p => ShortName(Path.GetFileNameWithoutExtension(p)), static p => p, StringComparer.Ordinal);

    public static string ShortName(string assemblyOrProjectName)
    {
        ArgumentNullException.ThrowIfNull(assemblyOrProjectName);
        return assemblyOrProjectName.StartsWith(Prefix, StringComparison.Ordinal) ? assemblyOrProjectName[Prefix.Length..] : assemblyOrProjectName;
    }

    /// <summary>
    /// 工程文件里对本应用其他包的 ProjectReference（短名）。
    /// </summary>
    public static IReadOnlyList<string> PackageReferencesOf(string projectPath)
    {
        var document = XDocument.Load(projectPath);
        return document.Descendants("ProjectReference")
            .Select(static e => e.Attribute("Include")?.Value ?? string.Empty)
            .Select(static include => Path.GetFileNameWithoutExtension(include.Replace('\\', '/')))
            .Where(static name => name.StartsWith(Prefix, StringComparison.Ordinal))
            .Select(ShortName)
            .ToList();
    }

    private static string Locate()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "XiHan.BasicApp.slnx")) && Directory.Exists(Path.Combine(directory.FullName, "props")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Cannot locate the backend-next directory from " + AppContext.BaseDirectory);
    }
}
