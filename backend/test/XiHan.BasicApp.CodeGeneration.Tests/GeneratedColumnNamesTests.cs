// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.CodeGeneration.Domain.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 基类托管列名集合测试。
/// </summary>
/// <remarks>
/// 该集合是"模板判定 <c>col.IsBaseColumn</c>"与"推断引擎标 <c>IsCommon</c>"的单一事实源：
/// 漏项会让基类已经声明的列被当业务列再生成一遍到实体/DTO 里（重复成员，编译不过）；
/// 误判会让真正的业务列在产物中整列消失。
/// </remarks>
public sealed class GeneratedColumnNamesTests
{
    /// <summary>
    /// 基类托管列共 14 项：主键两写法 + 租户 + 行版本 + 软删 + 三组审计列（时间/操作者Id/操作者名）。
    /// 数量与内容变化意味着实体基类变了，必须同步复核模板与推断引擎。
    /// </summary>
    [Fact]
    public void BaseColumns_ShouldContainAllFourteenManagedColumns()
    {
        string[] expected =
        [
            "BasicId", "Id", "TenantId", "RowVersion", "IsDeleted",
            "CreatedTime", "CreatedId", "CreatedBy",
            "ModifiedTime", "ModifiedId", "ModifiedBy",
            "DeletedTime", "DeletedId", "DeletedBy"
        ];

        Assert.Equal(expected.Length, GeneratedColumnNames.BaseColumns.Count);
        Assert.All(expected, name => Assert.Contains(name, GeneratedColumnNames.BaseColumns, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 集合内的每一项，都必须能被判定函数认出来（含库里带下划线与全大写的真实写法），
    /// 集合被删项时本用例立即变红。
    /// </summary>
    [Fact]
    public void IsBaseColumn_EveryBaseColumnShouldBeRecognizedInAllWritingStyles()
    {
        foreach (var name in GeneratedColumnNames.BaseColumns)
        {
            Assert.True(GeneratedColumnNames.IsBaseColumn(name), name);
            Assert.True(GeneratedColumnNames.IsBaseColumn(name.ToLowerInvariant()), name);
            Assert.True(GeneratedColumnNames.IsBaseColumn(name.ToUpperInvariant()), name);
        }
    }

    /// <summary>
    /// 判定忽略大小写且忽略下划线：库里的 <c>Basic_Id</c> / <c>created_time</c> 与属性名 <c>BasicId</c> 等价，
    /// 否则带下划线命名策略的库表会把基类列全部当成业务列。
    /// </summary>
    /// <param name="columnName">数据库列名或属性名</param>
    [Theory]
    [InlineData("BasicId")]
    [InlineData("Basic_Id")]
    [InlineData("basic_id")]
    [InlineData("BASICID")]
    [InlineData("BASIC_ID")]
    [InlineData("Created_Time")]
    [InlineData("created_time")]
    [InlineData("Is_Deleted")]
    [InlineData("Tenant_Id")]
    [InlineData("Row_Version")]
    public void IsBaseColumn_ShouldIgnoreCaseAndUnderscore(string columnName)
    {
        Assert.True(GeneratedColumnNames.IsBaseColumn(columnName));
    }

    /// <summary>
    /// 业务列必须返回 false，且不得出现前缀式误判：
    /// <c>CreatedTimeExtra</c> 只是名字以基类列开头，仍是业务列。
    /// </summary>
    /// <param name="columnName">业务列名</param>
    [Theory]
    [InlineData("ProductName")]
    [InlineData("Sort")]
    [InlineData("Remark")]
    [InlineData("CreatedTimeExtra")]
    [InlineData("MyBasicId")]
    [InlineData("TenantIdRef")]
    [InlineData("Deleted")]
    [InlineData("Version")]
    public void IsBaseColumn_BusinessColumnShouldReturnFalse(string columnName)
    {
        Assert.False(GeneratedColumnNames.IsBaseColumn(columnName));
    }

    /// <summary>
    /// 空值与纯空白返回 false 且不抛异常（外部库可能给出空列名）。
    /// </summary>
    /// <param name="columnName">空值或空白列名</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("_")]
    public void IsBaseColumn_BlankShouldReturnFalse(string columnName)
    {
        Assert.False(GeneratedColumnNames.IsBaseColumn(columnName));
    }

    /// <summary>
    /// null 列名同样安全降级为 false，避免整条导入链因一个空列名崩掉。
    /// </summary>
    [Fact]
    public void IsBaseColumn_NullShouldReturnFalse()
    {
        Assert.False(GeneratedColumnNames.IsBaseColumn(null!));
    }
}
