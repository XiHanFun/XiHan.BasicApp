// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Moq;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Enums;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.BasicApp.Saas.Domain.Enums;
using XiHan.Framework.Data.SqlSugar.Connections;

namespace XiHan.BasicApp.CodeGeneration.Tests;

/// <summary>
/// 数据源领域服务的不变量测试。
/// </summary>
/// <remarks>
/// 锁定四条最贵的约定：
/// <list type="number">
/// <item>密钥（口令 / 连接串）入库必须是加密态，且更新时留空表示"不修改"——前端脱敏字段回传空值，
/// 若按空值覆盖会把已保存的口令抹掉。</item>
/// <item>唯一默认源：置默认时必须把旧默认源置回非默认，否则运行期会有两个"默认"。</item>
/// <item>改动 / 停用 / 删除后必须注销动态连接记账，否则进程内一直复用旧连接直到重启。</item>
/// <item>取连接信息 fail-closed：数据源不存在或已停用一律抛异常，绝不静默回落到本系统主库。</item>
/// </list>
/// 全部用例只经 Moq 与内存实体，不建立任何真实数据库连接。
/// </remarks>
public sealed class CodeGenDataSourceDomainServiceTests
{
    private readonly Mock<ICodeGenDataSourceRepository> _repository = new();
    private readonly Mock<IDynamicConnectionRegistrar> _registrar = new();
    private readonly CodeGenDataSourceDomainService _service;

    /// <summary>
    /// 构造被测领域服务（仓储与连接注册器均为宽松 Mock）。
    /// </summary>
    public CodeGenDataSourceDomainServiceTests()
    {
        _repository
            .Setup(repository => repository.AddAsync(It.IsAny<SysCodeGenDataSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysCodeGenDataSource entity, CancellationToken _) => entity);
        _repository
            .Setup(repository => repository.UpdateAsync(It.IsAny<SysCodeGenDataSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((SysCodeGenDataSource entity, CancellationToken _) => entity);
        _repository
            .Setup(repository => repository.DeleteAsync(It.IsAny<SysCodeGenDataSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _service = new CodeGenDataSourceDomainService(_repository.Object, _registrar.Object);
    }

    /// <summary>
    /// 构造一条创建命令（各字段给足合法默认值，测试只覆盖关心的那一个）。
    /// </summary>
    /// <param name="sourceName">数据源名称</param>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="password">口令明文</param>
    /// <param name="connectionString">显式连接串明文</param>
    /// <param name="connectionTimeout">连接超时（秒）</param>
    /// <param name="isDefault">是否默认源</param>
    /// <param name="status">状态</param>
    /// <param name="sourceDescription">描述</param>
    private static CodeGenDataSourceCreateCommand CreateCommand(
        string sourceName = "本地库",
        DatabaseType databaseType = DatabaseType.MySql,
        string? password = "p@ss",
        string? connectionString = null,
        int connectionTimeout = 15,
        bool isDefault = false,
        EnableStatus status = EnableStatus.Enabled,
        string? sourceDescription = null)
    {
        return new CodeGenDataSourceCreateCommand(
            sourceName,
            sourceDescription,
            databaseType,
            "127.0.0.1",
            3306,
            "demo",
            "root",
            password,
            connectionString,
            null,
            connectionTimeout,
            isDefault,
            status,
            0,
            null);
    }

    /// <summary>
    /// 构造一条更新命令。
    /// </summary>
    /// <param name="basicId">主键</param>
    /// <param name="sourceName">数据源名称</param>
    /// <param name="password">口令明文（null/空白表示不修改）</param>
    /// <param name="connectionString">显式连接串明文（null/空白表示不修改）</param>
    /// <param name="isDefault">是否默认源</param>
    /// <param name="remark">备注</param>
    private static CodeGenDataSourceUpdateCommand UpdateCommand(
        long basicId = 1,
        string sourceName = "本地库",
        string? password = null,
        string? connectionString = null,
        bool isDefault = false,
        string? remark = null)
    {
        return new CodeGenDataSourceUpdateCommand(
            basicId,
            sourceName,
            null,
            DatabaseType.MySql,
            "127.0.0.1",
            3306,
            "demo",
            "root",
            password,
            connectionString,
            null,
            0,
            isDefault,
            0,
            remark);
    }

    /// <summary>
    /// 构造一条已落库的数据源实体。
    /// </summary>
    /// <param name="id">主键</param>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="status">状态</param>
    /// <param name="isDefault">是否默认源</param>
    private static SysCodeGenDataSource Existing(
        long id = 1,
        DatabaseType databaseType = DatabaseType.MySql,
        EnableStatus status = EnableStatus.Enabled,
        bool isDefault = false)
    {
        return CodeGenerationTestHelper.WithId(
            new SysCodeGenDataSource
            {
                SourceName = "旧名",
                DatabaseType = databaseType,
                Host = "db.internal",
                Port = 5432,
                DatabaseName = "demo",
                UserName = "root",
                ConnectionTimeout = 20,
                Status = status,
                IsDefault = isDefault,
                Remark = "原备注"
            },
            id);
    }

    /// <summary>
    /// 创建时命令为空必须直接拒绝。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_NullCommandShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _service.CreateDataSourceAsync(null!));
    }

    /// <summary>
    /// 创建时令牌已取消必须在触库前抛出。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_CancelledTokenShouldThrowBeforeRepository()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.CreateDataSourceAsync(CreateCommand(), cts.Token));

        _repository.Verify(
            repository => repository.ExistsNameAsync(It.IsAny<string>(), It.IsAny<long?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    /// <summary>
    /// 数据库类型取未定义枚举值必须拒绝（枚举越界会一路带到连接串拼装才炸）。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_UndefinedDatabaseTypeShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateDataSourceAsync(CreateCommand(databaseType: (DatabaseType)99)));

        Assert.Equal("DatabaseType", exception.ParamName);
    }

    /// <summary>
    /// 状态取未定义枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_UndefinedStatusShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateDataSourceAsync(CreateCommand(status: (EnableStatus)7)));

        Assert.Equal("Status", exception.ParamName);
    }

    /// <summary>
    /// 数据源名称为空或纯空白必须拒绝（null 抛的是派生的 ArgumentNullException）。
    /// </summary>
    /// <param name="sourceName">非法名称</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateDataSourceAsync_BlankSourceNameShouldThrow(string? sourceName)
    {
        await Assert.ThrowsAnyAsync<ArgumentException>(
            () => _service.CreateDataSourceAsync(CreateCommand(sourceName: sourceName!)));
    }

    /// <summary>
    /// 数据源名称超过 100 字必须拒绝，且提示词落在名称上。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_TooLongSourceNameShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateDataSourceAsync(CreateCommand(sourceName: new string('名', 101))));

        Assert.Contains("数据源名称不能超过 100 个字符。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 名称恰好 100 字属于边界内，必须放行。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_SourceNameAtMaxLengthShouldPass()
    {
        var name = new string('名', 100);

        var result = await _service.CreateDataSourceAsync(CreateCommand(sourceName: name));

        Assert.Equal(name, result.DataSource.SourceName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 描述超过 500 字必须拒绝。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_TooLongDescriptionShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.CreateDataSourceAsync(CreateCommand(sourceDescription: new string('述', 501))));

        Assert.Contains("数据源描述不能超过 500 个字符。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 名称重复必须拒绝（创建时排除项传 null，即与全部记录比对）。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_DuplicateNameShouldThrow()
    {
        _repository
            .Setup(repository => repository.ExistsNameAsync("本地库", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateDataSourceAsync(CreateCommand()));

        Assert.Equal("数据源名称已存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 口令与连接串入库必须是加密态，绝不能出现明文。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_SecretsShouldBeStoredEncrypted()
    {
        var result = await _service.CreateDataSourceAsync(
            CreateCommand(password: "p@ss", connectionString: "Server=127.0.0.1;Database=demo;"));

        Assert.NotNull(result.DataSource.Password);
        Assert.NotEqual("p@ss", result.DataSource.Password, StringComparer.Ordinal);
        Assert.NotNull(result.DataSource.ConnectionString);
        Assert.NotEqual("Server=127.0.0.1;Database=demo;", result.DataSource.ConnectionString, StringComparer.Ordinal);
    }

    /// <summary>
    /// 空白口令与空白连接串归 null，不写入空串密文。
    /// </summary>
    /// <param name="secret">空白密钥</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateDataSourceAsync_BlankSecretsShouldBecomeNull(string? secret)
    {
        var result = await _service.CreateDataSourceAsync(CreateCommand(password: secret, connectionString: secret));

        Assert.Null(result.DataSource.Password);
        Assert.Null(result.DataSource.ConnectionString);
    }

    /// <summary>
    /// 连接超时非正数时回落到 30 秒默认值。
    /// </summary>
    /// <param name="timeout">入参超时</param>
    /// <param name="expected">期望落库值</param>
    [Theory]
    [InlineData(0, 30)]
    [InlineData(-1, 30)]
    [InlineData(int.MinValue, 30)]
    [InlineData(1, 1)]
    [InlineData(600, 600)]
    public async Task CreateDataSourceAsync_NonPositiveTimeoutShouldFallBackTo30(int timeout, int expected)
    {
        var result = await _service.CreateDataSourceAsync(CreateCommand(connectionTimeout: timeout));

        Assert.Equal(expected, result.DataSource.ConnectionTimeout);
    }

    /// <summary>
    /// 置为默认源时必须把已存在的默认源改回非默认，保证默认源唯一。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_IsDefaultShouldClearPreviousDefault()
    {
        var previous = Existing(id: 9, isDefault: true);
        _repository
            .Setup(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(previous);

        await _service.CreateDataSourceAsync(CreateCommand(isDefault: true));

        Assert.False(previous.IsDefault);
        _repository.Verify(repository => repository.UpdateAsync(previous, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 非默认源不得触碰既有默认源。
    /// </summary>
    [Fact]
    public async Task CreateDataSourceAsync_NonDefaultShouldNotTouchPreviousDefault()
    {
        await _service.CreateDataSourceAsync(CreateCommand(isDefault: false));

        _repository.Verify(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 更新时主键必须大于 0。
    /// </summary>
    /// <param name="basicId">非法主键</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(long.MinValue)]
    public async Task UpdateDataSourceAsync_NonPositiveIdShouldThrow(long basicId)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateDataSourceAsync(UpdateCommand(basicId: basicId)));

        Assert.Contains("数据源主键必须大于 0。", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 更新时重名判定必须排除自身。
    /// </summary>
    [Fact]
    public async Task UpdateDataSourceAsync_DuplicateNameShouldThrowAndExcludeSelf()
    {
        _repository
            .Setup(repository => repository.ExistsNameAsync("本地库", 1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateDataSourceAsync(UpdateCommand()));

        Assert.Equal("数据源名称已存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新不存在的数据源必须报"不存在"。
    /// </summary>
    [Fact]
    public async Task UpdateDataSourceAsync_MissingDataSourceShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateDataSourceAsync(UpdateCommand()));

        Assert.Equal("数据源不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新时留空口令表示"不修改"，已保存的密文必须原样保留。
    /// </summary>
    /// <param name="secret">留空的密钥入参</param>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateDataSourceAsync_BlankSecretShouldKeepStoredCipher(string? secret)
    {
        var existing = Existing();
        existing.Password = "OLD-CIPHER";
        existing.ConnectionString = "OLD-CONNECTION-CIPHER";
        _repository
            .Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.UpdateDataSourceAsync(UpdateCommand(password: secret, connectionString: secret));

        Assert.Equal("OLD-CIPHER", result.DataSource.Password, StringComparer.Ordinal);
        Assert.Equal("OLD-CONNECTION-CIPHER", result.DataSource.ConnectionString, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新时给出新口令则加密覆盖。
    /// </summary>
    [Fact]
    public async Task UpdateDataSourceAsync_NewSecretShouldBeEncryptedAndOverwrite()
    {
        var existing = Existing();
        existing.Password = "OLD-CIPHER";
        _repository
            .Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        var result = await _service.UpdateDataSourceAsync(UpdateCommand(password: "newSecret"));

        Assert.NotEqual("OLD-CIPHER", result.DataSource.Password, StringComparer.Ordinal);
        Assert.NotEqual("newSecret", result.DataSource.Password, StringComparer.Ordinal);
    }

    /// <summary>
    /// 更新成功后必须注销该数据源的动态连接记账。
    /// </summary>
    [Fact]
    public async Task UpdateDataSourceAsync_ShouldUnregisterDynamicConnection()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(42L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Existing(id: 42));

        await _service.UpdateDataSourceAsync(UpdateCommand(basicId: 42));

        _registrar.Verify(registrar => registrar.Unregister("42"), Times.Once);
    }

    /// <summary>
    /// 由非默认改为默认时，旧默认源必须让位。
    /// </summary>
    [Fact]
    public async Task UpdateDataSourceAsync_PromotingToDefaultShouldClearPreviousDefault()
    {
        var current = Existing(id: 3, isDefault: false);
        var previous = Existing(id: 8, isDefault: true);
        _repository.Setup(repository => repository.GetByIdAsync(3L, It.IsAny<CancellationToken>())).ReturnsAsync(current);
        _repository.Setup(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(previous);

        var result = await _service.UpdateDataSourceAsync(UpdateCommand(basicId: 3, isDefault: true));

        Assert.False(previous.IsDefault);
        Assert.True(result.DataSource.IsDefault);
    }

    /// <summary>
    /// 本身已是默认源时不得把自己置回非默认。
    /// </summary>
    [Fact]
    public async Task UpdateDataSourceAsync_AlreadyDefaultShouldNotDemoteItself()
    {
        var current = Existing(id: 3, isDefault: true);
        _repository.Setup(repository => repository.GetByIdAsync(3L, It.IsAny<CancellationToken>())).ReturnsAsync(current);
        _repository.Setup(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>())).ReturnsAsync(current);

        var result = await _service.UpdateDataSourceAsync(UpdateCommand(basicId: 3, isDefault: true));

        Assert.True(result.DataSource.IsDefault);
        _repository.Verify(repository => repository.GetDefaultAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    /// <summary>
    /// 状态变更取未定义枚举值必须拒绝。
    /// </summary>
    [Fact]
    public async Task UpdateDataSourceStatusAsync_UndefinedStatusShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _service.UpdateDataSourceStatusAsync(new CodeGenDataSourceStatusChangeCommand(1, (EnableStatus)5, null)));

        Assert.Equal("Status", exception.ParamName);
    }

    /// <summary>
    /// 状态变更时备注留空表示"不修改"，原备注必须保留。
    /// </summary>
    [Fact]
    public async Task UpdateDataSourceStatusAsync_BlankRemarkShouldKeepExisting()
    {
        var existing = Existing();
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.UpdateDataSourceStatusAsync(
            new CodeGenDataSourceStatusChangeCommand(1, EnableStatus.Disabled, "   "));

        Assert.Equal(EnableStatus.Disabled, result.DataSource.Status);
        Assert.Equal("原备注", result.DataSource.Remark, StringComparer.Ordinal);
        _registrar.Verify(registrar => registrar.Unregister("1"), Times.Once);
    }

    /// <summary>
    /// 删除不存在的数据源必须报"不存在"。
    /// </summary>
    [Fact]
    public async Task DeleteDataSourceAsync_MissingDataSourceShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteDataSourceAsync(1));

        Assert.Equal("数据源不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 仓储删除返回 false 时必须抛出，不得静默当成功。
    /// </summary>
    [Fact]
    public async Task DeleteDataSourceAsync_RepositoryFailureShouldThrow()
    {
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(Existing());
        _repository
            .Setup(repository => repository.DeleteAsync(It.IsAny<SysCodeGenDataSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.DeleteDataSourceAsync(1));

        Assert.Equal("数据源删除失败。", exception.Message, StringComparer.Ordinal);
        _registrar.Verify(registrar => registrar.Unregister(It.IsAny<string>()), Times.Never);
    }

    /// <summary>
    /// 删除成功后必须注销动态连接。
    /// </summary>
    [Fact]
    public async Task DeleteDataSourceAsync_SuccessShouldUnregisterDynamicConnection()
    {
        _repository.Setup(repository => repository.GetByIdAsync(7L, It.IsAny<CancellationToken>())).ReturnsAsync(Existing(id: 7));

        await _service.DeleteDataSourceAsync(7);

        _registrar.Verify(registrar => registrar.Unregister("7"), Times.Once);
    }

    /// <summary>
    /// 取连接信息时数据源不存在必须抛异常，不得回落主库。
    /// </summary>
    [Fact]
    public async Task GetConnectionInfoAsync_MissingDataSourceShouldThrow()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetConnectionInfoAsync(1));

        Assert.Equal("数据源不存在。", exception.Message, StringComparer.Ordinal);
    }

    /// <summary>
    /// 已停用的数据源必须拒绝取连接信息（fail-closed）。
    /// </summary>
    [Fact]
    public async Task GetConnectionInfoAsync_DisabledDataSourceShouldThrow()
    {
        var existing = Existing(status: EnableStatus.Disabled);
        existing.SourceName = "报表库";
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GetConnectionInfoAsync(1));

        Assert.Contains("已停用", exception.Message, StringComparison.Ordinal);
        Assert.Contains("报表库", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// 数据库类型 → SqlSugar DbType 的映射必须逐项对上。
    /// </summary>
    /// <param name="databaseType">数据源数据库类型</param>
    /// <param name="expected">期望的 SqlSugar 类型</param>
    [Theory]
    [InlineData(DatabaseType.MySql, DbType.MySql)]
    [InlineData(DatabaseType.SqlServer, DbType.SqlServer)]
    [InlineData(DatabaseType.PostgreSql, DbType.PostgreSQL)]
    [InlineData(DatabaseType.Oracle, DbType.Oracle)]
    [InlineData(DatabaseType.Sqlite, DbType.Sqlite)]
    public async Task GetConnectionInfoAsync_ShouldMapDatabaseTypeToSqlSugarDbType(DatabaseType databaseType, DbType expected)
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(5L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Existing(id: 5, databaseType: databaseType));

        var info = await _service.GetConnectionInfoAsync(5);

        Assert.Equal(expected, info.DbType);
        Assert.Equal("5", info.ConfigId, StringComparer.Ordinal);
        Assert.Equal("旧名", info.SourceName, StringComparer.Ordinal);
    }

    /// <summary>
    /// 各方言的连接串拼装格式必须稳定（没有显式连接串时按主机/端口/库/账号拼）。
    /// </summary>
    /// <param name="databaseType">数据库类型</param>
    /// <param name="expected">期望连接串</param>
    [Theory]
    [InlineData(DatabaseType.MySql, "Server=db.internal;Port=5432;Database=demo;Uid=root;Pwd=;Connection Timeout=20;")]
    [InlineData(DatabaseType.SqlServer, "Server=db.internal,5432;Database=demo;User Id=root;Password=;Connect Timeout=20;TrustServerCertificate=true;")]
    [InlineData(DatabaseType.PostgreSql, "Host=db.internal;Port=5432;Database=demo;Username=root;Password=;Timeout=20;")]
    [InlineData(DatabaseType.Oracle, "Data Source=db.internal:5432/demo;User Id=root;Password=;Connection Timeout=20;")]
    [InlineData(DatabaseType.Sqlite, "Data Source=demo;")]
    public async Task GetConnectionInfoAsync_ShouldBuildDialectSpecificConnectionString(DatabaseType databaseType, string expected)
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Existing(databaseType: databaseType));

        var info = await _service.GetConnectionInfoAsync(1);

        Assert.Equal(expected, info.ConnectionString, StringComparer.Ordinal);
    }

    /// <summary>
    /// 连接超时非正数时连接串按 30 秒兜底。
    /// </summary>
    [Fact]
    public async Task GetConnectionInfoAsync_NonPositiveTimeoutShouldFallBackTo30()
    {
        var existing = Existing();
        existing.ConnectionTimeout = 0;
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var info = await _service.GetConnectionInfoAsync(1);

        Assert.Contains("Connection Timeout=30;", info.ConnectionString, StringComparison.Ordinal);
    }

    /// <summary>
    /// 显式连接串优先于逐段拼装，且必须能被解密回明文（加解密同口径）。
    /// </summary>
    [Fact]
    public async Task GetConnectionInfoAsync_ExplicitConnectionStringShouldWinAndRoundTrip()
    {
        const string PlainConnectionString = "Server=other.host;Port=1;Database=other;Uid=u;Pwd=x;";
        var created = await _service.CreateDataSourceAsync(CreateCommand(connectionString: PlainConnectionString));
        var stored = CodeGenerationTestHelper.WithId(created.DataSource, 11);
        _repository.Setup(repository => repository.GetByIdAsync(11L, It.IsAny<CancellationToken>())).ReturnsAsync(stored);

        var info = await _service.GetConnectionInfoAsync(11);

        Assert.Equal(PlainConnectionString, info.ConnectionString, StringComparer.Ordinal);
    }

    /// <summary>
    /// 历史遗留的明文连接串解密失败时按明文兼容返回，不得让连接测试整体不可用。
    /// </summary>
    [Fact]
    public async Task GetConnectionInfoAsync_UndecryptableConnectionStringShouldFallBackToRawValue()
    {
        var existing = Existing();
        existing.ConnectionString = "Server=legacy;Database=plain;";
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var info = await _service.GetConnectionInfoAsync(1);

        Assert.Equal("Server=legacy;Database=plain;", info.ConnectionString, StringComparer.Ordinal);
    }

    /// <summary>
    /// 库里存了未支持的数据库类型（历史脏数据）时必须显式报不支持。
    /// </summary>
    [Fact]
    public async Task GetConnectionInfoAsync_UnsupportedDatabaseTypeShouldThrowNotSupported()
    {
        _repository
            .Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Existing(databaseType: (DatabaseType)99));

        await Assert.ThrowsAsync<NotSupportedException>(() => _service.GetConnectionInfoAsync(1));
    }

    /// <summary>
    /// 连接探测失败时必须回写失败结果而不是冒泡异常。
    /// </summary>
    /// <remarks>
    /// 用未支持的数据库类型让连接串拼装在探测入口就抛错，从而在不触网、不连库的前提下
    /// 走完"失败 → 回写 LastTest*"这条分支。
    /// </remarks>
    [Fact]
    public async Task TestConnectionAsync_FailureShouldBeRecordedInsteadOfThrown()
    {
        var existing = Existing(databaseType: (DatabaseType)99);
        _repository.Setup(repository => repository.GetByIdAsync(1L, It.IsAny<CancellationToken>())).ReturnsAsync(existing);

        var result = await _service.TestConnectionAsync(1);

        Assert.False(result.Success);
        Assert.NotNull(result.Message);
        Assert.StartsWith("连接失败：", result.Message, StringComparison.Ordinal);
        Assert.True(result.ElapsedMilliseconds >= 0);
        Assert.False(existing.LastTestResult);
        Assert.NotNull(existing.LastTestTime);
        Assert.Equal(result.Message, existing.LastTestMessage, StringComparer.Ordinal);
        _repository.Verify(repository => repository.UpdateAsync(existing, It.IsAny<CancellationToken>()), Times.Once);
    }

    /// <summary>
    /// 连接测试的主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task TestConnectionAsync_NonPositiveIdShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.TestConnectionAsync(0));
    }

    /// <summary>
    /// 取连接信息的主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task GetConnectionInfoAsync_NonPositiveIdShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetConnectionInfoAsync(-5));
    }

    /// <summary>
    /// 删除的主键必须大于 0。
    /// </summary>
    [Fact]
    public async Task DeleteDataSourceAsync_NonPositiveIdShouldThrow()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.DeleteDataSourceAsync(0));
    }
}
