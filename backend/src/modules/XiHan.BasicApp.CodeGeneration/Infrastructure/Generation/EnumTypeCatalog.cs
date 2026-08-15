// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using Microsoft.Extensions.Logging;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

/// <summary>
/// 枚举类型目录实现（反射一次、进程内缓存）
/// </summary>
/// <remarks>
/// 扫描面覆盖全部 XiHan 程序集：生成期只在服务端消费、不经端点暴露，
/// 与枚举元数据端点的收窄口径无关。
/// </remarks>
public sealed class EnumTypeCatalog : IEnumTypeCatalog
{
    private readonly IReadOnlyDictionary<string, EnumTypeFacts> _byFullName;
    private readonly IReadOnlyDictionary<string, EnumTypeFacts> _byShortName;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="logger">日志（短名重复时告警）</param>
    public EnumTypeCatalog(ILogger<EnumTypeCatalog> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var byFullName = new Dictionary<string, EnumTypeFacts>(StringComparer.Ordinal);
        var shortNameGroups = new Dictionary<string, List<Type>>(StringComparer.Ordinal);

        foreach (var type in EnumerateEnumTypes())
        {
            byFullName[type.FullName!] = ToFacts(type);
            if (!shortNameGroups.TryGetValue(type.Name, out var group))
            {
                group = [];
                shortNameGroups[type.Name] = group;
            }

            group.Add(type);
        }

        var byShortName = new Dictionary<string, EnumTypeFacts>(StringComparer.Ordinal);
        foreach (var (shortName, group) in shortNameGroups)
        {
            var distinct = group.Distinct().ToList();
            if (distinct.Count == 1)
            {
                byShortName[shortName] = ToFacts(distinct[0]);
                continue;
            }

            // 短名重复：优先取 XiHan.BasicApp 下的；仍并列则整组不可按短名解析（按全名仍可解析）
            var preferred = distinct
                .Where(static type => type.Namespace?.StartsWith("XiHan.BasicApp.", StringComparison.Ordinal) == true)
                .ToList();
            if (preferred.Count == 1)
            {
                byShortName[shortName] = ToFacts(preferred[0]);
                continue;
            }

            logger.LogWarning(
                "枚举短名 {ShortName} 在多个命名空间下重复（{Namespaces}），列配置请填写全名。",
                shortName,
                string.Join(", ", distinct.Select(static type => type.Namespace)));
        }

        _byFullName = byFullName;
        _byShortName = byShortName;
    }

    /// <summary>
    /// 解析枚举类型名（全名优先，回退短名）
    /// </summary>
    /// <param name="enumTypeName">枚举类型名</param>
    /// <param name="facts">解析结果</param>
    /// <returns>解析成功返回 true</returns>
    public bool TryResolve(string? enumTypeName, out EnumTypeFacts facts)
    {
        facts = default!;
        if (string.IsNullOrWhiteSpace(enumTypeName))
        {
            return false;
        }

        var key = enumTypeName.Trim();
        return _byFullName.TryGetValue(key, out facts!) || _byShortName.TryGetValue(key, out facts!);
    }

    /// <summary>
    /// 枚举已加载 XiHan 程序集中的 public 顶层枚举
    /// </summary>
    private static IEnumerable<Type> EnumerateEnumTypes()
    {
        foreach (var assembly in LoadXiHanAssemblies())
        {
            if (assembly.IsDynamic)
            {
                continue;
            }

            var assemblyName = assembly.GetName().Name;
            if (assemblyName is null || !assemblyName.StartsWith("XiHan", StringComparison.Ordinal))
            {
                continue;
            }

            Type?[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                assemblyTypes = ex.Types;
            }
            catch (FileNotFoundException)
            {
                continue;
            }

            foreach (var type in assemblyTypes)
            {
                if (type is { IsEnum: true, IsPublic: true, IsNested: false, FullName: not null, Namespace: not null })
                {
                    yield return type;
                }
            }
        }
    }

    /// <summary>
    /// 取全部 XiHan 程序集（沿引用链强制加载）
    /// </summary>
    /// <remarks>
    /// 只读 <c>AppDomain.GetAssemblies()</c> 会漏掉尚未被触碰的模块程序集——CLR 按需加载，
    /// 目录是单例、首次构建即定型，漏掉的模块此后永远解析不到。
    /// </remarks>
    private static List<Assembly> LoadXiHanAssemblies()
    {
        var visited = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<Assembly>();
        var pending = new Queue<Assembly>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            pending.Enqueue(assembly);
        }

        while (pending.Count > 0)
        {
            var assembly = pending.Dequeue();
            var name = assembly.GetName().Name;
            if (assembly.IsDynamic || name is null
                || !name.StartsWith("XiHan", StringComparison.Ordinal)
                || !visited.Add(name))
            {
                continue;
            }

            result.Add(assembly);

            foreach (var reference in assembly.GetReferencedAssemblies())
            {
                if (reference.Name is null
                    || !reference.Name.StartsWith("XiHan", StringComparison.Ordinal)
                    || visited.Contains(reference.Name))
                {
                    continue;
                }

                try
                {
                    pending.Enqueue(Assembly.Load(reference));
                }
                catch (Exception ex) when (ex is FileNotFoundException or BadImageFormatException)
                {
                    // 未部署该程序集时忽略
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 类型 → 事实
    /// </summary>
    private static EnumTypeFacts ToFacts(Type type)
    {
        return new EnumTypeFacts(type.Name, type.Namespace!, Enum.GetNames(type).FirstOrDefault() ?? string.Empty);
    }
}
