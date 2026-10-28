// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using SqlSugar;
using XiHan.BasicApp.Host;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.BasicApp.Settings.Domain.Entities;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Persistence.Abstractions;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 建表列映射：宿主装进来的 BasicApp 实体与框架数据库记录，写了 <c>[LongText]</c> 的字符串列建成长文本、<c>[StringLength(n)]</c> 建成长度 n，
/// 不被 <see cref="SqlSugarRuntime"/> 的缺省长度盖掉；没注解的字符串列是 <see cref="SqlSugarRuntime.DefaultStringLength"/>。
/// SQLite 不校验 varchar 长度，这一条只靠映射元数据守住（PostgreSQL 上超长写入报 22001）。
/// </summary>
public sealed class PersistenceMappingTests
{
    [Fact]
    public void AnnotatedStringColumns_KeepTheirDeclaredType()
    {
        using var client = new SqlSugarClient(SqlSugarConventions.Apply(Configure()));
        var annotated = HostAssemblies()
            .SelectMany(LoadableTypes)
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(property => property.PropertyType == typeof(string) && property.CanWrite && IsAnnotated(property))
                .Select(property => (Type: type, Property: property)))
            .ToList();
        Assert.Contains(annotated, entry => entry.Type == typeof(SysUserSetting) && entry.Property.Name == nameof(SysUserSetting.Value));
        Assert.Contains(annotated, entry => entry.Type == typeof(SysConfig) && entry.Property.Name == nameof(SysConfig.Value));
        Assert.Contains(annotated, entry => entry.Type.Assembly.GetName().Name!.StartsWith("XiHan.Framework.", StringComparison.Ordinal));

        foreach (var (type, property) in annotated)
        {
            var column = client.EntityMaintenance.GetEntityInfo(type).Columns.Single(c => c.PropertyName == property.Name);
            var label = $"{type.FullName}.{property.Name}: {column.DataType}({column.Length})";
            if (property.GetCustomAttribute<StringLengthAttribute>() is { MaximumLength: > 0 } stringLength)
            {
                Assert.True(column.Length == stringLength.MaximumLength, label);
            }
            else if (property.GetCustomAttribute<MaxLengthAttribute>() is { Length: > 0 } maxLength)
            {
                Assert.True(column.Length == maxLength.Length, label);
            }
            else
            {
                Assert.True(column.DataType == StaticConfig.CodeFirst_BigString && column.Length == 0, label);
            }
        }

        var plain = client.EntityMaintenance.GetEntityInfo<SysUserSetting>().Columns.Single(c => c.PropertyName == nameof(SysUserSetting.Key));
        Assert.Equal(SqlSugarRuntime.DefaultStringLength, plain.Length);
    }

    private static bool IsAnnotated(PropertyInfo property) =>
        property.IsDefined(typeof(LongTextAttribute), inherit: true)
        || property.IsDefined(typeof(StringLengthAttribute), inherit: true)
        || property.IsDefined(typeof(MaxLengthAttribute), inherit: true);

    private static ConnectionConfig Configure()
    {
        var config = new ConnectionConfig();
        var binding = new BindingValue("db", "connection:relational", "Host=127.0.0.1;Database=mapping", new Dictionary<string, string>(StringComparer.Ordinal) { ["provider"] = DatabaseProviders.PostgreSql });
        SqlSugarRuntime.Configure(Maybe.Some(binding), config);
        return config;
    }

    // 宿主程序集引用闭包里的 XiHan.* 程序集：BasicApp 各包与它们带进来的框架数据库记录
    private static IEnumerable<Assembly> HostAssemblies()
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var pending = new Queue<Assembly>([typeof(BasicAppHost).Assembly]);
        while (pending.TryDequeue(out var assembly))
        {
            if (!seen.Add(assembly.GetName().Name!))
            {
                continue;
            }

            yield return assembly;
            foreach (var reference in assembly.GetReferencedAssemblies().Where(name => name.Name!.StartsWith("XiHan.", StringComparison.Ordinal)))
            {
                pending.Enqueue(Assembly.Load(reference));
            }
        }
    }

    private static IEnumerable<Type> LoadableTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            return exception.Types.OfType<Type>();
        }
    }
}
