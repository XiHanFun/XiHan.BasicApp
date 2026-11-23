// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Nodes;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 把示例插件 CrmPlugin 的构建产物装成插件包
/// </summary>
/// <remarks>
/// 根上放两份清单、前端文案与页面描述，其余构建产物放进 <c>runtimes/&lt;当前运行时标识&gt;/</c>；旁挂清单的 <c>runtimes</c> 换成当前运行时标识，与包里有的一致
/// 构建产物取与测试同一配置的输出：工程目录下的 <c>bin/&lt;配置&gt;/&lt;目标框架&gt;</c>，或 <c>--artifacts-path</c> 布局里的 <c>bin/CrmPlugin/&lt;配置&gt;</c>；测试工程引用示例插件只为让它先构建
/// </remarks>
internal static class CrmPluginPackage
{
    /// <summary>
    /// 短名
    /// </summary>
    public const string Name = "crm";

    /// <summary>
    /// 清单 ID
    /// </summary>
    public const string ManifestId = "xihan.samples.crm";

    /// <summary>
    /// 查看客户
    /// </summary>
    public const string Read = "crm.crm-customers.read";

    /// <summary>
    /// 新建客户
    /// </summary>
    public const string Create = "crm.crm-customers.create";

    /// <summary>
    /// 修改客户
    /// </summary>
    public const string Update = "crm.crm-customers.update";

    /// <summary>
    /// 删除客户
    /// </summary>
    public const string Delete = "crm.crm-customers.delete";

    /// <summary>
    /// 留在包根上的文件与目录
    /// </summary>
    private static readonly string[] _rootEntries = ["xihan.manifest.json", "basicapp.plugin.json", "locales/", "pages/"];

    /// <summary>
    /// 示例插件的构建产物目录
    /// </summary>
    public static string Output { get; } = Locate();

    /// <summary>
    /// 装出插件包
    /// </summary>
    public static byte[] Build()
    {
        var runtime = RuntimeInformation.RuntimeIdentifier;
        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.EnumerateFiles(Output, "*", SearchOption.AllDirectories))
            {
                var path = Path.GetRelativePath(Output, file).Replace('\\', '/');
                var root = _rootEntries.Any(e => e.EndsWith('/') ? path.StartsWith(e, StringComparison.Ordinal) : path == e);
                var content = path == "basicapp.plugin.json" ? Descriptor(file, runtime) : File.ReadAllBytes(file);
                using var stream = archive.CreateEntry(root ? path : $"runtimes/{runtime}/{path}").Open();
                stream.Write(content);
            }
        }

        return buffer.ToArray();
    }

    /// <summary>
    /// 旁挂清单，<c>runtimes</c> 换成当前运行时标识
    /// </summary>
    private static byte[] Descriptor(string file, string runtime)
    {
        var descriptor = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
        descriptor["runtimes"] = new JsonArray(runtime);
        return Encoding.UTF8.GetBytes(descriptor.ToJsonString());
    }

    /// <summary>
    /// 按测试自己的输出目录找到示例插件同一配置的输出
    /// </summary>
    private static string Locate()
    {
        var output = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        var backend = output;
        while (backend is not null && !File.Exists(Path.Combine(backend.FullName, "XiHan.BasicApp.slnx")))
        {
            backend = backend.Parent;
        }

        if (backend is null)
        {
            throw new InvalidOperationException("Cannot locate the backend directory from " + AppContext.BaseDirectory);
        }

        // 工程目录布局：test/<工程>/bin/<配置>/<目标框架>；artifacts 布局：bin/<工程>/<配置>
        string[] candidates =
        [
            Path.Combine(backend.FullName, "samples", "CrmPlugin", "bin", output.Parent!.Name, output.Name),
            Path.Combine(output.Parent.Parent!.FullName, "CrmPlugin", output.Name),
        ];
        return candidates.FirstOrDefault(static c => File.Exists(Path.Combine(c, "xihan.manifest.json")))
            ?? throw new InvalidOperationException($"The sample plugin has not been built; looked in {string.Join(", ", candidates)}.");
    }
}
