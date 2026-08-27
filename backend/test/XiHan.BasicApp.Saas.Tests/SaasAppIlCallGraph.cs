// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace XiHan.BasicApp.Saas.Tests;

/// <summary>
/// 方法体 IL 调用图探针（测试专用）。
/// </summary>
/// <remarks>
/// 有一类约定光看特性看不出来，只能看"方法体里到底调没调那一下"：
/// 写路径有没有真的调缓存失效器、种子器有没有真的切到平台租户上下文、有没有先查后写。
/// 本探针直接解码方法体 IL 取出被调方法；async 方法的方法体只是启动状态机，
/// 因此会自动跟进其 <c>MoveNext</c>。
/// <para>
/// 步进依赖 <see cref="OpCodes"/> 反射出的操作码长度表，遇到无法识别的字节即停止扫描，
/// 宁可漏报也不产生错位噪声。
/// </para>
/// </remarks>
internal static class SaasAppIlCallGraph
{
    /// <summary>
    /// 操作码表（按数值索引，用于按正确长度步进 IL）。
    /// </summary>
    private static readonly Dictionary<short, OpCode> OpCodeMap = typeof(OpCodes)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.FieldType == typeof(OpCode))
        .Select(field => (OpCode)field.GetValue(null)!)
        .GroupBy(opCode => opCode.Value)
        .ToDictionary(group => group.Key, group => group.First());

    /// <summary>
    /// 判断方法（含其在指定类型内调用的其它方法）是否触达满足条件的调用。
    /// </summary>
    /// <param name="method">起点方法。</param>
    /// <param name="owner">允许继续展开的宿主类型（含其编译器生成的嵌套状态机/闭包）。</param>
    /// <param name="predicate">目标调用判定。</param>
    /// <returns>是否触达。</returns>
    internal static bool Reaches(MethodBase method, Type owner, Func<MethodBase, bool> predicate)
    {
        return Reaches(method, owner, predicate, [], 0);
    }

    private static bool Reaches(MethodBase method, Type owner, Func<MethodBase, bool> predicate, HashSet<MethodBase> visited, int depth)
    {
        if (depth > 4 || !visited.Add(method))
        {
            return false;
        }

        foreach (var callee in ResolveCalledMethods(method))
        {
            if (predicate(callee))
            {
                return true;
            }

            if (IsOwnedBy(callee.DeclaringType, owner)
                && Reaches(callee, owner, predicate, visited, depth + 1))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 判断类型是否属于宿主类型自身或其（含编译器生成的）嵌套类型。
    /// </summary>
    /// <param name="candidate">待判定类型。</param>
    /// <param name="owner">宿主类型。</param>
    /// <returns>是否属于。</returns>
    private static bool IsOwnedBy(Type? candidate, Type owner)
    {
        while (candidate is not null)
        {
            if (candidate == owner)
            {
                return true;
            }

            candidate = candidate.DeclaringType;
        }

        return false;
    }

    /// <summary>
    /// 解析方法体 IL 中被调用的方法（async 方法自动改看其状态机的 MoveNext）。
    /// </summary>
    /// <param name="method">方法。</param>
    /// <returns>被调方法集合。</returns>
    internal static IEnumerable<MethodBase> ResolveCalledMethods(MethodBase method)
    {
        var target = method;
        if (method.GetCustomAttribute<AsyncStateMachineAttribute>()?.StateMachineType is { } machineType)
        {
            var moveNext = machineType.GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (moveNext is not null)
            {
                target = moveNext;
            }
        }

        var il = target.GetMethodBody()?.GetILAsByteArray();
        if (il is null)
        {
            yield break;
        }

        var module = target.Module;
        var typeArguments = target.DeclaringType?.IsGenericType == true
            ? target.DeclaringType.GetGenericArguments()
            : null;

        var position = 0;
        while (position < il.Length)
        {
            short code = il[position];
            if (code == 0xFE && position + 1 < il.Length)
            {
                code = (short)(0xFE00 | il[position + 1]);
                position += 2;
            }
            else
            {
                position += 1;
            }

            if (!OpCodeMap.TryGetValue(code, out var opCode))
            {
                // 步长一旦错位，继续扫描只会产生噪声，直接停下（宁可漏报）
                yield break;
            }

            if (opCode.OperandType == OperandType.InlineMethod && position + 4 <= il.Length)
            {
                MethodBase? resolved;
                try
                {
                    resolved = module.ResolveMethod(BitConverter.ToInt32(il, position), typeArguments, null);
                }
                catch (ArgumentException)
                {
                    resolved = null;
                }

                if (resolved is not null)
                {
                    yield return resolved;
                }
            }

            position += OperandSize(opCode, il, position);
        }
    }

    /// <summary>
    /// 按操作数类型计算当前指令的操作数字节数。
    /// </summary>
    /// <param name="opCode">操作码。</param>
    /// <param name="il">IL 字节流。</param>
    /// <param name="position">操作数起始位置。</param>
    /// <returns>操作数字节数。</returns>
    private static int OperandSize(OpCode opCode, byte[] il, int position)
    {
        return opCode.OperandType switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => position + 4 <= il.Length ? 4 + (4 * BitConverter.ToInt32(il, position)) : il.Length,
            _ => 4
        };
    }
}
