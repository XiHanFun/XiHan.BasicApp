// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// 一个枚举类型的事实。
/// </summary>
/// <param name="ShortName">短名。</param>
/// <param name="Namespace">命名空间。</param>
/// <param name="Assembly">所在程序集名（生成的包要引用它）。</param>
/// <param name="Members">成员名，按声明顺序。</param>
public sealed record CodeGenEnumFacts(string ShortName, string Namespace, string Assembly, IReadOnlyList<string> Members)
{
    /// <summary>
    /// 第一个成员（表单的缺省值）。
    /// </summary>
    public string DefaultMember => Members.Count > 0 ? Members[0] : string.Empty;

    /// <summary>
    /// 是否启停枚举：有 <c>Active</c> 与 <c>Disabled</c> 两个成员（6.0 各包状态枚举的写法）。
    /// </summary>
    public bool IsActiveDisabled => Members.Contains("Active", StringComparer.Ordinal) && Members.Contains("Disabled", StringComparer.Ordinal);
}

/// <summary>
/// 已装载的 XiHan 程序集里的公开枚举。
/// </summary>
/// <remarks>
/// 照 4.x：列配置的枚举类型按全名或短名解析；短名重复时优先 <c>XiHan.BasicApp.</c> 下的，仍并列即只能按全名解析。
/// </remarks>
public sealed class CodeGenEnumCatalog
{
    private const string AppPrefix = "XiHan.BasicApp.";

    private readonly Lazy<(IReadOnlyDictionary<string, CodeGenEnumFacts> ByFullName, IReadOnlyDictionary<string, CodeGenEnumFacts> ByShortName)> _index = new(Build);

    /// <summary>
    /// 按全名或短名解析。
    /// </summary>
    /// <param name="enumTypeName">枚举类型名。</param>
    /// <param name="facts">解析出的事实。</param>
    /// <returns>是否解析成功。</returns>
    public bool TryResolve(string? enumTypeName, out CodeGenEnumFacts facts)
    {
        facts = null!;
        if (string.IsNullOrWhiteSpace(enumTypeName))
        {
            return false;
        }

        var key = enumTypeName.Trim();
        var (byFullName, byShortName) = _index.Value;
        return byFullName.TryGetValue(key, out facts!) || byShortName.TryGetValue(key, out facts!);
    }

    private static (IReadOnlyDictionary<string, CodeGenEnumFacts>, IReadOnlyDictionary<string, CodeGenEnumFacts>) Build()
    {
        var types = LoadAssemblies().SelectMany(LoadableEnums).Distinct().ToList();
        var byFullName = types.ToDictionary(static t => t.FullName!, Facts, StringComparer.Ordinal);
        var byShortName = new Dictionary<string, CodeGenEnumFacts>(StringComparer.Ordinal);
        foreach (var group in types.GroupBy(static t => t.Name, StringComparer.Ordinal))
        {
            var candidates = group.Count() == 1 ? group.ToList() : group.Where(static t => t.Namespace!.StartsWith(AppPrefix, StringComparison.Ordinal)).ToList();
            if (candidates.Count == 1)
            {
                byShortName[group.Key] = Facts(candidates[0]);
            }
        }

        return (byFullName, byShortName);
    }

    private static CodeGenEnumFacts Facts(Type type)
        => new(type.Name, type.Namespace!, type.Assembly.GetName().Name!, Enum.GetNames(type));

    private static IEnumerable<Type> LoadableEnums(Assembly assembly)
    {
        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types;
        }

        return types.Where(static t => t is { IsEnum: true, IsPublic: true, IsNested: false, FullName: not null, Namespace: not null }).Select(static t => t!);
    }

    // 已装载的 XiHan 程序集与它们引用的 XiHan 程序集
    private static List<Assembly> LoadAssemblies()
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<Assembly>();
        var pending = new Queue<Assembly>(AppDomain.CurrentDomain.GetAssemblies());
        while (pending.TryDequeue(out var assembly))
        {
            var name = assembly.GetName().Name;
            if (assembly.IsDynamic || name is null || !name.StartsWith("XiHan", StringComparison.Ordinal) || !visited.Add(name))
            {
                continue;
            }

            result.Add(assembly);
            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name is not null && reference.Name.StartsWith("XiHan", StringComparison.Ordinal) && !visited.Contains(reference.Name))
                {
                    try
                    {
                        pending.Enqueue(Assembly.Load(reference));
                    }
                    catch (FileNotFoundException)
                    {
                        // 没有部署的引用不算
                    }
                }
            }
        }

        return result;
    }
}
