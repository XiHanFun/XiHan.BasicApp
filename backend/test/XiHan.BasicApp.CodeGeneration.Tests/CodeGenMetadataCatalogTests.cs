// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Extensions.Logging.Abstractions;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 实体元数据目录与枚举类型目录的反射扫描结果测试。
/// </summary>
/// <remarks>
/// 两个目录都是单例、构造时反射一次、此后进程内不变，因此"扫漏了"是静默的：
/// 表名还原不了就退化成外部表（类名/命名空间全靠猜），枚举解析不了就退化成没有选项来源的下拉。
/// 这里用本模块自己的实体与枚举做锚点——它们必然在扫描面内，扫描口径一旦收窄就会红。
/// 目录构造开销较大且结果不可变，用例间共享同一实例，不引入可变静态状态。
/// </remarks>
public sealed class CodeGenMetadataCatalogTests
{
    private static readonly Lazy<EntityMetadataCatalog> SharedEntityCatalog = new(() => new EntityMetadataCatalog());

    private static readonly Lazy<EnumTypeCatalog> SharedEnumCatalog =
        new(() => new EnumTypeCatalog(NullLogger<EnumTypeCatalog>.Instance));

    private static EntityMetadataCatalog EntityCatalog => SharedEntityCatalog.Value;

    private static EnumTypeCatalog EnumCatalog => SharedEnumCatalog.Value;

    /// <summary>
    /// 已注册实体的表名必须被还原成 <c>[SugarTable]</c> 上声明的真实大小写。
    /// </summary>
    /// <param name="dbTableName">数据库返回的表名（大小写任意）</param>
    [Theory]
    [InlineData("sys_codegen_table")]
    [InlineData("SYS_CODEGEN_TABLE")]
    [InlineData("Sys_CodeGen_Table")]
    public void ResolveTable_ShouldRestoreDeclaredCasing(string dbTableName)
    {
        Assert.Equal("Sys_CodeGen_Table", EntityCatalog.ResolveTable(dbTableName), StringComparer.Ordinal);
    }

    /// <summary>
    /// 外部库的表不在目录中，必须原样返回而不是抛异常或返回空。
    /// </summary>
    [Fact]
    public void ResolveTable_UnknownTableShouldBeReturnedAsIs()
    {
        Assert.Equal("some_external_table", EntityCatalog.ResolveTable("some_external_table"), StringComparer.Ordinal);
    }

    /// <summary>
    /// 空白表名原样返回，不得进入字典查询。
    /// </summary>
    /// <param name="dbTableName">空白表名</param>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveTable_BlankNameShouldBeReturnedAsIs(string dbTableName)
    {
        Assert.Equal(dbTableName, EntityCatalog.ResolveTable(dbTableName), StringComparer.Ordinal);
    }

    /// <summary>
    /// 精确命中的表，逻辑名与真实名一致。
    /// </summary>
    [Fact]
    public void ResolveLogical_ExactMatchShouldReturnRealName()
    {
        Assert.Equal("Sys_CodeGen_Template", EntityCatalog.ResolveLogical("sys_codegen_template"), StringComparer.Ordinal);
    }

    /// <summary>
    /// 既不精确命中也不是分片的表名原样返回。
    /// </summary>
    [Fact]
    public void ResolveLogical_UnknownTableShouldBeReturnedAsIs()
    {
        Assert.Equal("external_orders", EntityCatalog.ResolveLogical("external_orders"), StringComparer.Ordinal);
    }

    /// <summary>
    /// 列名必须还原成 <c>[SugarColumn]</c> 上声明的真实大小写。
    /// </summary>
    /// <param name="dbColumnName">数据库返回的列名</param>
    /// <param name="expected">期望还原结果</param>
    [Theory]
    [InlineData("table_name", "Table_Name")]
    [InlineData("TABLE_NAME", "Table_Name")]
    [InlineData("user_modified_fields", "User_Modified_Fields")]
    public void ResolveColumn_ShouldRestoreDeclaredColumnCasing(string dbColumnName, string expected)
    {
        Assert.Equal(expected, EntityCatalog.ResolveColumn("Sys_CodeGen_Table", dbColumnName), StringComparer.Ordinal);
    }

    /// <summary>
    /// 表未注册或列未声明时，列名原样返回。
    /// </summary>
    [Fact]
    public void ResolveColumn_UnknownTableOrColumnShouldBeReturnedAsIs()
    {
        Assert.Equal("any_col", EntityCatalog.ResolveColumn("external_orders", "any_col"), StringComparer.Ordinal);
        Assert.Equal("not_declared", EntityCatalog.ResolveColumn("Sys_CodeGen_Table", "not_declared"), StringComparer.Ordinal);
    }

    /// <summary>
    /// 空白入参原样返回列名。
    /// </summary>
    [Fact]
    public void ResolveColumn_BlankArgumentsShouldBeReturnedAsIs()
    {
        Assert.Equal("col", EntityCatalog.ResolveColumn("   ", "col"), StringComparer.Ordinal);
        Assert.Equal("   ", EntityCatalog.ResolveColumn("Sys_CodeGen_Table", "   "), StringComparer.Ordinal);
    }

    /// <summary>
    /// 项目内的表必须能取到实体类型，这是"输入最小化"对本系统表近乎零配置的前提。
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="expectedTypeName">期望的实体类型名</param>
    [Theory]
    [InlineData("sys_codegen_table", nameof(SysCodeGenTable))]
    [InlineData("Sys_CodeGen_TableColumn", nameof(SysCodeGenTableColumn))]
    [InlineData("SYS_CODEGEN_TEMPLATE", nameof(SysCodeGenTemplate))]
    [InlineData("sys_codegen_datasource", nameof(SysCodeGenDataSource))]
    [InlineData("sys_codegen_history", nameof(SysCodeGenHistory))]
    public void TryGetEntityType_RegisteredTableShouldResolveEntityType(string tableName, string expectedTypeName)
    {
        Assert.True(EntityCatalog.TryGetEntityType(tableName, out var entityType));
        Assert.Equal(expectedTypeName, entityType.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// 外部库的表取不到实体类型，且出参回落为 object 而不是 null。
    /// </summary>
    /// <param name="tableName">未注册的表名</param>
    [Theory]
    [InlineData("external_orders")]
    [InlineData("")]
    [InlineData("   ")]
    public void TryGetEntityType_UnknownTableShouldReturnFalse(string tableName)
    {
        Assert.False(EntityCatalog.TryGetEntityType(tableName, out var entityType));
        Assert.Equal(typeof(object), entityType);
    }

    /// <summary>
    /// 非分表实体不得被判成分表基础名，其"看起来像分片"的名字也解析不出基础名。
    /// </summary>
    [Fact]
    public void SplitTableApis_ShouldNotMatchNonSplitEntities()
    {
        Assert.False(EntityCatalog.IsSplitBase("Sys_CodeGen_Table"));
        Assert.False(EntityCatalog.IsSplitBase("   "));
        Assert.False(EntityCatalog.TryResolveSplitShard("sys_codegen_table_20260101", out var baseName));
        Assert.Equal(string.Empty, baseName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 空白表名不得被当成分片。
    /// </summary>
    [Fact]
    public void TryResolveSplitShard_BlankNameShouldReturnFalse()
    {
        Assert.False(EntityCatalog.TryResolveSplitShard("   ", out var baseName));
        Assert.Equal(string.Empty, baseName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 枚举目录的日志依赖为空必须直接拒绝（短名重复时要靠它告警）。
    /// </summary>
    [Fact]
    public void EnumTypeCatalog_NullLoggerShouldThrow()
    {
        Assert.Throws<ArgumentNullException>(() => new EnumTypeCatalog(null!));
    }

    /// <summary>
    /// 按全名解析枚举，必须给出短名、命名空间与首个成员名。
    /// </summary>
    [Fact]
    public void TryResolve_FullNameShouldReturnFacts()
    {
        Assert.True(EnumCatalog.TryResolve(typeof(TemplateType).FullName, out var facts));

        Assert.Equal("TemplateType", facts.ShortName, StringComparer.Ordinal);
        Assert.Equal(typeof(TemplateType).Namespace, facts.Namespace, StringComparer.Ordinal);
        Assert.Equal("Single", facts.DefaultMemberName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 短名唯一时也能解析，方便列配置只填短名。
    /// </summary>
    /// <param name="shortName">枚举短名</param>
    /// <param name="expectedDefaultMember">期望的首个成员名</param>
    [Theory]
    [InlineData("TemplateType", "Single")]
    [InlineData("GenerationScope", "All")]
    [InlineData("DictSelectorType", "DictSelector")]
    [InlineData("ArtifactWriteMode", "AlwaysOverwrite")]
    public void TryResolve_UniqueShortNameShouldReturnFacts(string shortName, string expectedDefaultMember)
    {
        Assert.True(EnumCatalog.TryResolve(shortName, out var facts));

        Assert.Equal(shortName, facts.ShortName, StringComparer.Ordinal);
        Assert.Equal(expectedDefaultMember, facts.DefaultMemberName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 首个成员按枚举值升序取，与表单默认值口径一致。
    /// </summary>
    [Fact]
    public void TryResolve_DefaultMemberShouldBeTheLowestValuedMember()
    {
        Assert.True(EnumCatalog.TryResolve(typeof(TemplateEngine).FullName, out var facts));

        Assert.Equal("Scriban", facts.DefaultMemberName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 两端空白先裁掉再解析。
    /// </summary>
    [Fact]
    public void TryResolve_ShouldTrimInput()
    {
        Assert.True(EnumCatalog.TryResolve("  TemplateType  ", out var facts));

        Assert.Equal("TemplateType", facts.ShortName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 解析是 Ordinal 精确匹配：大小写不同一律不认，避免误挂到别的枚举上。
    /// </summary>
    /// <param name="enumTypeName">大小写不符的类型名</param>
    [Theory]
    [InlineData("templatetype")]
    [InlineData("TEMPLATETYPE")]
    public void TryResolve_ShouldBeOrdinalExactMatch(string enumTypeName)
    {
        Assert.False(EnumCatalog.TryResolve(enumTypeName, out _));
    }

    /// <summary>
    /// 空白或未知类型名解析失败，由调用方降级处理。
    /// </summary>
    /// <param name="enumTypeName">空白或未知类型名</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Some.Unknown.EnumType")]
    public void TryResolve_BlankOrUnknownShouldReturnFalse(string? enumTypeName)
    {
        Assert.False(EnumCatalog.TryResolve(enumTypeName, out _));
    }
}
