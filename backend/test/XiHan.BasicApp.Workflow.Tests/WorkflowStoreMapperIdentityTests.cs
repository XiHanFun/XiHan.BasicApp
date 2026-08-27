// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Workflow.Infrastructure.Stores;
using XiHan.Framework.Workflow.Abstractions.Exceptions;

namespace XiHan.BasicApp.Workflow.Tests;

/// <summary>
/// 框架标识与数据库主键互转的锁定测试。
/// </summary>
/// <remarks>
/// <see cref="WorkflowStoreMapper.ParseId"/> 是引擎标识与实体主键之间唯一的桥：
/// 解析口径一旦放宽（接受符号、空白、千分位），脏标识会被静默写成错误主键；
/// 一旦不抛 <see cref="WorkflowException"/>，底层的 FormatException/OverflowException
/// 会穿透到 API 变成 500，而不是可纠正的业务错误。
/// </remarks>
public sealed class WorkflowStoreMapperIdentityTests
{
    /// <summary>
    /// 纯数字字符串必须解析为对应主键，含边界值 0 与 long 最大值。
    /// </summary>
    /// <param name="id">待解析的标识文本。</param>
    /// <param name="expected">期望主键。</param>
    [Theory]
    [InlineData("0", 0L)]
    [InlineData("1", 1L)]
    [InlineData("100200300400500", 100200300400500L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    [InlineData("0000000000000000001", 1L)]
    public void ParseId_PlainDigits_ShouldReturnMatchingKey(string id, long expected)
    {
        Assert.Equal(expected, WorkflowStoreMapper.ParseId(id));
    }

    /// <summary>
    /// NumberStyles.None 语义必须锁死：带符号、带空白、带千分位、科学计数、十六进制、小数一律拒绝。
    /// </summary>
    /// <param name="id">被拒绝的脏标识文本。</param>
    [Theory]
    [InlineData("+1")]
    [InlineData("-1")]
    [InlineData(" 1")]
    [InlineData("1 ")]
    [InlineData(" 1 ")]
    [InlineData("1,000")]
    [InlineData("1e3")]
    [InlineData("0x1F")]
    [InlineData("1.0")]
    [InlineData("1_000")]
    public void ParseId_DirtyNumericText_ShouldThrowWorkflowException(string id)
    {
        var exception = Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ParseId(id));

        Assert.Contains("不是雪花数值字符串", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 非数值文本与空白文本必须抛出带提示词的工作流异常，而不是 FormatException。
    /// </summary>
    /// <param name="id">非数值标识文本。</param>
    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12a")]
    public void ParseId_NonNumericText_ShouldThrowWorkflowException(string id)
    {
        var exception = Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ParseId(id));

        Assert.Contains("不是雪花数值字符串", exception.Message, StringComparison.Ordinal);
        Assert.Contains(id, exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 超出 long 范围的全数字串必须转成工作流异常，而不是 OverflowException 穿透。
    /// </summary>
    /// <param name="id">溢出的数值文本。</param>
    [Theory]
    [InlineData("9223372036854775808")]
    [InlineData("99999999999999999999999999")]
    public void ParseId_OverflowDigits_ShouldThrowWorkflowException(string id)
    {
        var exception = Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ParseId(id));

        Assert.Contains("不是雪花数值字符串", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// null 入参钉住为工作流异常（long.TryParse(null) 返回 false），不得是 NullReferenceException。
    /// </summary>
    [Fact]
    public void ParseId_NullText_ShouldThrowWorkflowExceptionNotNullReference()
    {
        var exception = Assert.Throws<WorkflowException>(() => WorkflowStoreMapper.ParseId(null!));

        Assert.Contains("不是雪花数值字符串", exception.Message, StringComparison.Ordinal);
    }
}
