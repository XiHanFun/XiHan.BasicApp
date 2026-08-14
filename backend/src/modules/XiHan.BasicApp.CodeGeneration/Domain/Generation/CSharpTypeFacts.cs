// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.CodeGeneration.Domain.Generation;

/// <summary>
/// C# 类型名事实判定
/// </summary>
/// <remarks>
/// 供模板渲染上下文透出列级布尔。判定只认类型名文本，不做反射。
/// </remarks>
public static class CSharpTypeFacts
{
    /// <summary>
    /// 已收录的值类型名（含关键字别名与 BCL 名）
    /// </summary>
    private static readonly IReadOnlySet<string> ValueTypeNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "bool", "byte", "sbyte", "char", "short", "ushort", "int", "uint", "long", "ulong", "nint", "nuint",
        "float", "double", "decimal",
        "Boolean", "Byte", "SByte", "Char", "Int16", "UInt16", "Int32", "UInt32", "Int64", "UInt64",
        "Single", "Double", "Decimal",
        "DateTime", "DateTimeOffset", "DateOnly", "TimeOnly", "TimeSpan", "Guid"
    };

    /// <summary>
    /// 是否为值类型
    /// </summary>
    /// <param name="csharpType">C# 类型名，可带可空标注（如 <c>long</c>、<c>long?</c>）</param>
    /// <returns>已收录的值类型返回 true；未收录的类型按引用类型处理返回 false</returns>
    public static bool IsValueType(string? csharpType)
    {
        return ValueTypeNames.Contains(StripNullable(csharpType));
    }

    /// <summary>
    /// 是否为二进制类型
    /// </summary>
    /// <param name="csharpType">C# 类型名，可带可空标注</param>
    /// <returns>类型为 <c>byte[]</c> 时返回 true</returns>
    public static bool IsBinary(string? csharpType)
    {
        return StripNullable(csharpType) is "byte[]" or "Byte[]";
    }

    /// <summary>
    /// 去掉尾部可空标注
    /// </summary>
    private static string StripNullable(string? csharpType)
    {
        if (string.IsNullOrWhiteSpace(csharpType))
        {
            return string.Empty;
        }

        var value = csharpType.Trim();
        return value.EndsWith('?') ? value[..^1].TrimEnd() : value;
    }
}
