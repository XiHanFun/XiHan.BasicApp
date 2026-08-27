// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Reflection;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// C# 类型名事实判定测试。
/// </summary>
/// <remarks>
/// 模板据此透出列级布尔：值类型决定产物里是否对可空取 <c>.Value</c>，
/// 二进制类型是查询区跳过 byte[] 列的唯一判据。判定只认类型名文本、不做反射，
/// 因此收录表与大小写口径必须被逐条锁定。
/// </remarks>
public sealed class CSharpTypeFactsTests
{
    /// <summary>
    /// 收录表内的关键字别名与 BCL 名都必须判为值类型。
    /// </summary>
    /// <param name="csharpType">C# 类型名</param>
    [Theory]
    [InlineData("bool")]
    [InlineData("int")]
    [InlineData("long")]
    [InlineData("decimal")]
    [InlineData("double")]
    [InlineData("Boolean")]
    [InlineData("Int32")]
    [InlineData("Int64")]
    [InlineData("DateTime")]
    [InlineData("DateTimeOffset")]
    [InlineData("DateOnly")]
    [InlineData("TimeOnly")]
    [InlineData("TimeSpan")]
    [InlineData("Guid")]
    public void IsValueType_RegisteredTypeShouldReturnTrue(string csharpType)
    {
        Assert.True(CSharpTypeFacts.IsValueType(csharpType));
    }

    /// <summary>
    /// 未收录的类型（引用类型、集合、枚举短名、自定义类）一律按引用类型处理，
    /// 否则模板会对可空引用类型误取 <c>.Value</c> 而编译不过。
    /// </summary>
    /// <param name="csharpType">C# 类型名</param>
    [Theory]
    [InlineData("string")]
    [InlineData("object")]
    [InlineData("byte[]")]
    [InlineData("TemplateType")]
    [InlineData("SysCodeGenTable")]
    [InlineData("List<int>")]
    public void IsValueType_UnregisteredTypeShouldReturnFalse(string csharpType)
    {
        Assert.False(CSharpTypeFacts.IsValueType(csharpType));
    }

    /// <summary>
    /// 可空标注必须先被剥离，<c>long?</c> 与 <c>long</c> 判定完全一致；
    /// 尾随空白与 <c>?</c> 前空白也会被 Trim 掉（当前实现口径，锁定防止误改）。
    /// </summary>
    /// <param name="csharpType">带可空标注或空白的类型名</param>
    [Theory]
    [InlineData("long?")]
    [InlineData("long? ")]
    [InlineData(" long ")]
    [InlineData("long ?")]
    [InlineData("DateTimeOffset?")]
    public void IsValueType_NullableAndWhitespaceShouldBeStripped(string csharpType)
    {
        Assert.True(CSharpTypeFacts.IsValueType(csharpType));
    }

    /// <summary>
    /// 字符串比较是 Ordinal 大小写敏感：<c>INT</c> 与 <c>int</c> 不等价。
    /// 锁定该口径，避免后续被误改成忽略大小写而放行拼写错误的类型名。
    /// </summary>
    /// <param name="csharpType">大小写与收录表不一致的类型名</param>
    [Theory]
    [InlineData("INT")]
    [InlineData("Long")]
    [InlineData("BOOL")]
    [InlineData("guid")]
    [InlineData("datetime")]
    public void IsValueType_ShouldBeOrdinalCaseSensitive(string csharpType)
    {
        Assert.False(CSharpTypeFacts.IsValueType(csharpType));
    }

    /// <summary>
    /// 空值与纯空白按未收录处理返回 false，不得抛异常（列配置未填类型时会传空）。
    /// </summary>
    /// <param name="csharpType">空值或空白</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("?")]
    public void IsValueType_NullOrBlankShouldReturnFalse(string? csharpType)
    {
        Assert.False(CSharpTypeFacts.IsValueType(csharpType));
    }

    /// <summary>
    /// 只有 byte[] 家族判为二进制（含可空写法与 BCL 名），
    /// 这是查询区跳过二进制列的唯一判据，放宽会让 blob 列进搜索条件。
    /// </summary>
    /// <param name="csharpType">C# 类型名</param>
    /// <param name="expected">期望的二进制判定</param>
    [Theory]
    [InlineData("byte[]", true)]
    [InlineData("Byte[]", true)]
    [InlineData("byte[]?", true)]
    [InlineData("Byte[]?", true)]
    [InlineData(" byte[] ", true)]
    [InlineData("byte", false)]
    [InlineData("string", false)]
    [InlineData("BYTE[]", false)]
    [InlineData("byte[][]", false)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    public void IsBinary_ShouldOnlyMatchByteArray(string? csharpType, bool expected)
    {
        Assert.Equal(expected, CSharpTypeFacts.IsBinary(csharpType));
    }

    /// <summary>
    /// 收录表的完备性断言：表内每一项都必须判为值类型。
    /// 有人从表里删项（比如误删 DateTimeOffset）时本用例立即变红。
    /// </summary>
    [Fact]
    public void IsValueType_EveryRegisteredNameShouldBeConsistentWithTable()
    {
        var field = typeof(CSharpTypeFacts).GetField("ValueTypeNames", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(field);
        var names = Assert.IsAssignableFrom<IReadOnlySet<string>>(field.GetValue(null));

        Assert.NotEmpty(names);
        Assert.All(names, name => Assert.True(CSharpTypeFacts.IsValueType(name), $"收录表内的 {name} 未被判为值类型"));
        Assert.All(names, name => Assert.True(CSharpTypeFacts.IsValueType(name + "?"), $"收录表内的 {name}? 未被判为值类型"));

        // 关键成员必须在表内：缺一项就意味着一类列在产物中被当引用类型处理
        string[] mustContain = ["bool", "int", "long", "decimal", "double", "DateTimeOffset", "TimeSpan", "Guid"];
        Assert.All(mustContain, name => Assert.Contains(name, names, StringComparer.Ordinal));

        // string 绝不能进值类型表
        Assert.DoesNotContain("string", names, StringComparer.Ordinal);
    }
}
