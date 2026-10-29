// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using XiHan.BasicApp.Architecture.Tests.Model;

namespace XiHan.BasicApp.Architecture.Tests;

/// <summary>
/// 检查请求 / 响应 DTO 的写法。
/// </summary>
/// <remarks>
/// 有公开无参构造函数的类型不得用 <c>init</c> 属性——源生成的序列化把 init 属性当构造参数，JSON 里没写的成员会以 null 覆盖属性初始化器给的缺省值（分页的 <c>Filters = []</c> 变成 null）。
/// 位置记录（主构造函数）不受影响。
/// </remarks>
public sealed class ContractDtoTests
{
    [Fact]
    public void DtoTypes_WithParameterlessConstructor_DoNotUseInitOnlyProperties()
    {
        var violations = new List<string>();
        foreach (var type in Packages.Assemblies.SelectMany(static a => a.GetTypes()))
        {
            if (type.Namespace is null || type.IsAbstract || type.IsInterface || type.IsEnum
                || !(type.Namespace.Contains(".Operations.", StringComparison.Ordinal) || type.Namespace.EndsWith(".Contracts.Paging", StringComparison.Ordinal) || type.Namespace.EndsWith(".Contracts.Batch", StringComparison.Ordinal)))
            {
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) is null)
            {
                continue;
            }

            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                var setter = property.SetMethod;
                if (setter is not null && setter.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)))
                {
                    violations.Add($"{type.FullName}.{property.Name}");
                }
            }
        }

        Assert.True(violations.Count == 0, "init-only properties on DTOs with a parameterless constructor lose their defaults under source-generated deserialization; use 'set':" + Environment.NewLine + string.Join(Environment.NewLine, violations));
    }
}
