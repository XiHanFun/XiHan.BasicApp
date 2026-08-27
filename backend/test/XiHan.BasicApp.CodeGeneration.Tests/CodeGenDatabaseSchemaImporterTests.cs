// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Infrastructure.Generation;
using XiHan.Framework.Data.SqlSugar.Connections;
using XiHan.Framework.Data.SqlSugar.Metadata;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 数据库结构导入器测试。
/// </summary>
/// <remarks>
/// 导入器夹在框架元数据提供器与代码生成之间，负责三件容易出错的事：
/// <list type="number">
/// <item>多数据源按需注册：已注册则短路复用；未注册才解密取连接信息并登记，
/// 且数据源不存在/已停用时由领域服务 fail-closed 抛错，绝不回落主库。</item>
/// <item>大小写还原：部分数据库返回全小写表名/列名，驼峰信息丢失，必须对照已注册实体还原。</item>
/// <item>分表折叠：列表时把同一实体的物理分片折叠为基础逻辑名并去重；导入基础名时自动取最近分片的列结构。</item>
/// </list>
/// 全部用例基于 Moq 与内存假目录，不建立任何真实连接。
/// </remarks>
public sealed class CodeGenDatabaseSchemaImporterTests
{
    private readonly Mock<IDatabaseMetadataProvider> _metadataProvider = new();
    private readonly Mock<IDynamicConnectionRegistrar> _registrar = new();
    private readonly Mock<ICodeGenDataSourceDomainService> _dataSourceDomainService = new();
    private readonly FakeEntityMetadataCatalog _catalog = new();

    /// <summary>
    /// 构造被测导入器。
    /// </summary>
    private DatabaseSchemaImporter CreateImporter()
    {
        return new DatabaseSchemaImporter(
            _metadataProvider.Object,
            _registrar.Object,
            _dataSourceDomainService.Object,
            _catalog);
    }

    /// <summary>
    /// 构造一条表元数据。
    /// </summary>
    /// <param name="tableName">表名</param>
    /// <param name="description">表描述</param>
    /// <param name="columns">列元数据</param>
    private static DatabaseTableMetadata TableMetadata(
        string tableName,
        string? description = null,
        IReadOnlyList<DatabaseColumnMetadata>? columns = null)
    {
        return new DatabaseTableMetadata
        {
            TableName = tableName,
            TableDescription = description,
            Columns = columns ?? []
        };
    }

    /// <summary>
    /// 构造一条列元数据。
    /// </summary>
    /// <param name="columnName">列名</param>
    /// <param name="dataType">数据类型</param>
    /// <param name="isPrimaryKey">是否主键</param>
    /// <param name="isIdentity">是否自增</param>
    /// <param name="isNullable">是否可空</param>
    /// <param name="length">长度</param>
    /// <param name="scale">小数位</param>
    /// <param name="description">列注释</param>
    private static DatabaseColumnMetadata ColumnMetadata(
        string columnName,
        string dataType = "varchar",
        bool isPrimaryKey = false,
        bool isIdentity = false,
        bool isNullable = false,
        int? length = null,
        int? scale = null,
        string? description = null)
    {
        return new DatabaseColumnMetadata
        {
            ColumnName = columnName,
            DataType = dataType,
            IsPrimaryKey = isPrimaryKey,
            IsIdentity = isIdentity,
            IsNullable = isNullable,
            Length = length,
            Scale = scale,
            Description = description
        };
    }

    /// <summary>
    /// 挂上 GetTablesAsync 的返回。
    /// </summary>
    /// <param name="configId">期望的连接配置标识</param>
    /// <param name="tableNames">表名集合</param>
    private void GivenTables(string? configId, params string[] tableNames)
    {
        _metadataProvider
            .Setup(provider => provider.GetTablesAsync(configId, It.IsAny<CancellationToken>()))
            .ReturnsAsync([.. tableNames.Select(name => TableMetadata(name))]);
    }

    /// <summary>
    /// 未指定数据源时走主库：不注册任何动态连接，配置标识传 null。
    /// </summary>
    [Fact]
    public async Task ListTablesAsync_WithoutDataSourceShouldUsePrimaryDatabase()
    {
        GivenTables(null, "sys_product");

        var tables = await CreateImporter().ListTablesAsync();

        Assert.Equal(["sys_product"], tables);
        _registrar.Verify(registrar => registrar.Register(It.IsAny<DynamicConnectionDescriptor>()), Times.Never);
        _dataSourceDomainService.Verify(
            service => service.GetConnectionInfoAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 数据源已注册时直接复用，不再解密取连接信息。
    /// </summary>
    [Fact]
    public async Task ListTablesAsync_AlreadyRegisteredDataSourceShouldShortCircuit()
    {
        _registrar.Setup(registrar => registrar.IsRegistered("7")).Returns(true);
        GivenTables("7", "orders");

        var tables = await CreateImporter().ListTablesAsync("7");

        Assert.Equal(["orders"], tables);
        _dataSourceDomainService.Verify(
            service => service.GetConnectionInfoAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _registrar.Verify(registrar => registrar.Register(It.IsAny<DynamicConnectionDescriptor>()), Times.Never);
    }

    /// <summary>
    /// 数据源尚未注册时按需解密取连接信息并登记，随后用返回的 ConfigId 扫描。
    /// </summary>
    [Fact]
    public async Task ListTablesAsync_UnregisteredDataSourceShouldRegisterOnDemand()
    {
        _dataSourceDomainService
            .Setup(service => service.GetConnectionInfoAsync(7L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodeGenDataSourceConnectionInfo("7", DbType.PostgreSQL, "Host=x;", "报表库"));
        GivenTables("7", "orders");

        var tables = await CreateImporter().ListTablesAsync(" 7 ");

        Assert.Equal(["orders"], tables);
        _registrar.Verify(
            registrar => registrar.Register(It.Is<DynamicConnectionDescriptor>(descriptor =>
                descriptor.ConfigId == "7" && descriptor.DbType == DbType.PostgreSQL && descriptor.ConnectionString == "Host=x;")),
            Times.Once);
    }

    /// <summary>
    /// 数据源标识不是数字且未注册时必须显式报错，不得静默走主库。
    /// </summary>
    [Fact]
    public async Task ListTablesAsync_NonNumericDataSourceIdShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => CreateImporter().ListTablesAsync("abc"));

        Assert.Contains("数据源标识非法", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 表名必须还原为逻辑名（真实大小写 / 分表折叠）。
    /// </summary>
    [Fact]
    public async Task ListTablesAsync_ShouldResolveLogicalTableNames()
    {
        _catalog.LogicalNames["syscodegendatasource"] = "SysCodeGenDataSource";
        GivenTables(null, "syscodegendatasource", "external_table");

        var tables = await CreateImporter().ListTablesAsync();

        Assert.Equal(["SysCodeGenDataSource", "external_table"], tables);
    }

    /// <summary>
    /// 同一分表实体的多个物理分片折叠为一个逻辑名，并按首次出现顺序去重。
    /// </summary>
    [Fact]
    public async Task ListTablesAsync_ShouldFoldSplitShardsAndDeduplicate()
    {
        _catalog.LogicalNames["sysdifflog_20260601"] = "SysDiffLog";
        _catalog.LogicalNames["sysdifflog_20260602"] = "SysDiffLog";
        GivenTables(null, "sysdifflog_20260601", "sys_product", "sysdifflog_20260602");

        var tables = await CreateImporter().ListTablesAsync();

        Assert.Equal(["SysDiffLog", "sys_product"], tables);
    }

    /// <summary>
    /// 去重按大小写不敏感比对，避免同一张表以两种大小写各出现一次。
    /// </summary>
    [Fact]
    public async Task ListTablesAsync_DeduplicationShouldBeCaseInsensitive()
    {
        GivenTables(null, "Orders", "orders");

        var tables = await CreateImporter().ListTablesAsync();

        Assert.Equal(["Orders"], tables);
    }

    /// <summary>
    /// 表名为空必须直接拒绝（null 抛派生的 ArgumentNullException）。
    /// </summary>
    /// <param name="tableName">空白表名</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ImportTableAsync_BlankTableNameShouldThrow(string? tableName)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(() => CreateImporter().ImportTableAsync(tableName!));
    }

    /// <summary>
    /// 表不存在且不是分表基础名时返回 null，而不是抛异常。
    /// </summary>
    [Fact]
    public async Task ImportTableAsync_MissingTableShouldReturnNull()
    {
        var schema = await CreateImporter().ImportTableAsync("not_exists");

        Assert.Null(schema);
    }

    /// <summary>
    /// 导入时表名与列名必须还原为实体上的真实大小写。
    /// </summary>
    [Fact]
    public async Task ImportTableAsync_ShouldRestoreRealTableAndColumnCasing()
    {
        _catalog.RealNames["syscodegentemplate"] = "SysCodeGenTemplate";
        _catalog.ColumnNames[("SysCodeGenTemplate", "template_code")] = "Template_Code";
        _metadataProvider
            .Setup(provider => provider.GetTableAsync("syscodegentemplate", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TableMetadata("syscodegentemplate", "模板表", [ColumnMetadata("template_code")]));

        var schema = await CreateImporter().ImportTableAsync("syscodegentemplate");

        Assert.NotNull(schema);
        Assert.Equal("SysCodeGenTemplate", schema!.TableName, StringComparer.Ordinal);
        Assert.Equal("模板表", schema.TableComment, StringComparer.Ordinal);
        Assert.Equal("Template_Code", schema.Columns[0].ColumnName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 元数据查询用调用方传入的原始表名，还原只作用于产出结果。
    /// </summary>
    /// <remarks>
    /// 库里的物理表名可能是全小写，用还原后的驼峰名去查会查不到；这条顺序不能反。
    /// </remarks>
    [Fact]
    public async Task ImportTableAsync_ShouldQueryMetadataWithRawTableName()
    {
        _catalog.RealNames["syscodegentemplate"] = "SysCodeGenTemplate";
        _metadataProvider
            .Setup(provider => provider.GetTableAsync("syscodegentemplate", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TableMetadata("syscodegentemplate"));

        await CreateImporter().ImportTableAsync("syscodegentemplate");

        _metadataProvider.Verify(
            provider => provider.GetTableAsync("syscodegentemplate", null, It.IsAny<CancellationToken>()),
            Times.Once);
        _metadataProvider.Verify(
            provider => provider.GetTableAsync("SysCodeGenTemplate", null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 列元数据到列结构的映射必须逐字段对齐，必填由"非空且非自增"推出。
    /// </summary>
    [Fact]
    public async Task ImportTableAsync_ShouldMapColumnMetadataFieldByField()
    {
        _metadataProvider
            .Setup(provider => provider.GetTableAsync("orders", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TableMetadata("orders", columns:
            [
                ColumnMetadata("id", "bigint", isPrimaryKey: true, isIdentity: true, description: "主键"),
                ColumnMetadata("amount", "decimal", isNullable: true, length: 18, scale: 2, description: "金额")
            ]));

        var schema = await CreateImporter().ImportTableAsync("orders");

        Assert.NotNull(schema);
        var identity = schema!.Columns[0];
        Assert.True(identity.IsPrimaryKey);
        Assert.True(identity.IsIdentity);
        Assert.False(identity.IsRequired);
        Assert.Equal("bigint", identity.DbType, StringComparer.Ordinal);
        Assert.Equal("主键", identity.ColumnComment, StringComparer.Ordinal);

        var amount = schema.Columns[1];
        Assert.True(amount.IsNullable);
        Assert.False(amount.IsRequired);
        Assert.Equal(18, amount.Length);
        Assert.Equal(2, amount.DecimalDigits);
    }

    /// <summary>
    /// 非空且非自增的列必填。
    /// </summary>
    [Fact]
    public async Task ImportTableAsync_NonNullableNonIdentityColumnShouldBeRequired()
    {
        _metadataProvider
            .Setup(provider => provider.GetTableAsync("orders", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TableMetadata("orders", columns: [ColumnMetadata("code")]));

        var schema = await CreateImporter().ImportTableAsync("orders");

        Assert.True(schema!.Columns[0].IsRequired);
    }

    /// <summary>
    /// 主键列名取第一个主键列；没有主键时留空。
    /// </summary>
    [Fact]
    public async Task ImportTableAsync_PrimaryKeyColumnShouldBeFirstPrimaryKeyOrNull()
    {
        _metadataProvider
            .Setup(provider => provider.GetTableAsync("with_pk", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TableMetadata("with_pk", columns:
            [
                ColumnMetadata("code"),
                ColumnMetadata("id", "bigint", isPrimaryKey: true)
            ]));
        _metadataProvider
            .Setup(provider => provider.GetTableAsync("no_pk", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TableMetadata("no_pk", columns: [ColumnMetadata("code")]));

        var importer = CreateImporter();

        Assert.Equal("id", (await importer.ImportTableAsync("with_pk"))!.PrimaryKeyColumn, StringComparer.Ordinal);
        Assert.Null((await importer.ImportTableAsync("no_pk"))!.PrimaryKeyColumn);
    }

    /// <summary>
    /// 传入分表基础名（无物理表）时，自动扫描最近一个分片取列结构，表名仍产出逻辑名。
    /// </summary>
    [Fact]
    public async Task ImportTableAsync_SplitBaseShouldFallBackToLatestShard()
    {
        _catalog.RealNames["sysdifflog"] = "SysDiffLog";
        _catalog.SplitBases.Add("SysDiffLog");
        _catalog.Shards["sysdifflog_20260601"] = "SysDiffLog";
        _catalog.Shards["sysdifflog_20260602"] = "SysDiffLog";
        _metadataProvider
            .Setup(provider => provider.GetTableAsync("sysdifflog", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DatabaseTableMetadata?)null);
        GivenTables(null, "sysdifflog_20260601", "sysdifflog_20260602", "sys_product");
        _metadataProvider
            .Setup(provider => provider.GetTableAsync("sysdifflog_20260602", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TableMetadata("sysdifflog_20260602", columns: [ColumnMetadata("id", "bigint", isPrimaryKey: true)]));

        var schema = await CreateImporter().ImportTableAsync("sysdifflog");

        Assert.NotNull(schema);
        Assert.Equal("SysDiffLog", schema!.TableName, StringComparer.Ordinal);
        Assert.Single(schema.Columns);
        _metadataProvider.Verify(
            provider => provider.GetTableAsync("sysdifflog_20260601", null, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 分表基础名下一个分片都没有时返回 null，不得抛异常。
    /// </summary>
    [Fact]
    public async Task ImportTableAsync_SplitBaseWithoutAnyShardShouldReturnNull()
    {
        _catalog.RealNames["sysdifflog"] = "SysDiffLog";
        _catalog.SplitBases.Add("SysDiffLog");
        GivenTables(null, "sys_product");

        var schema = await CreateImporter().ImportTableAsync("sysdifflog");

        Assert.Null(schema);
    }

    /// <summary>
    /// 导入外部数据源的表时，连接注册同样按需发生。
    /// </summary>
    [Fact]
    public async Task ImportTableAsync_ShouldEnsureConnectionForExternalDataSource()
    {
        _dataSourceDomainService
            .Setup(service => service.GetConnectionInfoAsync(3L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CodeGenDataSourceConnectionInfo("3", DbType.MySql, "Server=x;", "外部库"));
        _metadataProvider
            .Setup(provider => provider.GetTableAsync("orders", "3", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TableMetadata("orders", columns: [ColumnMetadata("id", "bigint", isPrimaryKey: true)]));

        var schema = await CreateImporter().ImportTableAsync("orders", "3");

        Assert.NotNull(schema);
        _registrar.Verify(
            registrar => registrar.Register(It.Is<DynamicConnectionDescriptor>(descriptor => descriptor.ConfigId == "3")),
            Times.Once);
    }

    /// <summary>
    /// 数据源已停用时领域服务抛出的 fail-closed 异常必须原样冒泡，绝不吞掉后回落主库。
    /// </summary>
    [Fact]
    public async Task ImportTableAsync_DisabledDataSourceShouldPropagateFailClosedException()
    {
        _dataSourceDomainService
            .Setup(service => service.GetConnectionInfoAsync(3L, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("数据源「外部库」已停用，无法用于读取库表结构。"));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => CreateImporter().ImportTableAsync("orders", "3"));

        Assert.Contains("已停用", exception.Message, StringComparison.Ordinal);
        _metadataProvider.Verify(
            provider => provider.GetTableAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 内存假实体元数据目录：只按测试预置的字典回答，未命中一律原样返回。
    /// </summary>
    private sealed class FakeEntityMetadataCatalog : IEntityMetadataCatalog
    {
        /// <summary>表名真实大小写映射（键为小写物理名）</summary>
        public Dictionary<string, string> RealNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>逻辑名映射（键为小写物理名）</summary>
        public Dictionary<string, string> LogicalNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>列名真实大小写映射</summary>
        public Dictionary<(string Table, string Column), string> ColumnNames { get; } = [];

        /// <summary>分表基础名集合</summary>
        public HashSet<string> SplitBases { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>物理分片 → 基础名</summary>
        public Dictionary<string, string> Shards { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public string ResolveTable(string dbTableName)
            => RealNames.TryGetValue(dbTableName, out var real) ? real : dbTableName;

        /// <inheritdoc />
        public string ResolveLogical(string dbTableName)
            => LogicalNames.TryGetValue(dbTableName, out var logical) ? logical : ResolveTable(dbTableName);

        /// <inheritdoc />
        public string ResolveColumn(string realTableName, string dbColumnName)
            => ColumnNames.TryGetValue((realTableName, dbColumnName), out var real) ? real : dbColumnName;

        /// <inheritdoc />
        public bool IsSplitBase(string tableName) => SplitBases.Contains(tableName);

        /// <inheritdoc />
        public bool TryResolveSplitShard(string dbTableName, out string baseRealName)
        {
            if (Shards.TryGetValue(dbTableName, out var baseName))
            {
                baseRealName = baseName;
                return true;
            }

            baseRealName = string.Empty;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetEntityType(string tableName, out Type entityType)
        {
            entityType = typeof(object);
            return false;
        }
    }
}
