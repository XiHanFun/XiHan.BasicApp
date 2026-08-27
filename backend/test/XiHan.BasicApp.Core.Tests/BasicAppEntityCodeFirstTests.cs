// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;

namespace XiHan.BasicApp.Core.Tests;

/// <summary>
/// BasicApp 实体基类家族的 CodeFirst 落库形状测试。
/// </summary>
/// <remarks>
/// 前面几个文件断言的是"实体上的特性写对了没有"，这里再往前走一步：让 SqlSugar 真的把样例实体
/// 建成表，看落到数据库里的列名与约束是什么。这一层的价值在于，所有手写升级脚本的列名依据、
/// 以及"软删唯一索引末列附加 IsDeleted"这条约定的实际行为，只有建出表来才能验证。
/// <para>
/// 用的是临时 SQLite 文件库（<c>Path.GetTempPath()</c> + Guid 命名，Dispose 中关连接并删文件），
/// 不连任何外部数据库；样例实体全部定义在测试项目内，不触碰 src 下的生产实体。
/// </para>
/// </remarks>
public sealed class BasicAppEntityCodeFirstTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"xihan-core-entity-{Guid.NewGuid():N}.db");
    private readonly SqlSugarClient _client;

    /// <summary>
    /// 创建临时 SQLite 数据库并对三种形状的样例实体执行 CodeFirst 建表。
    /// </summary>
    public BasicAppEntityCodeFirstTests()
    {
        _client = new SqlSugarClient(new ConnectionConfig
        {
            ConnectionString = $"DataSource={_databasePath};Pooling=False",
            DbType = DbType.Sqlite,
            IsAutoCloseConnection = false
        });
        _client.CodeFirst.InitTables<CoreFullAuditedProbe>();
        _client.CodeFirst.InitTables<CoreCreationProbe>();
        _client.CodeFirst.InitTables<CoreAggregateRootTableProbe>();
    }

    /// <summary>
    /// 完整审计实体建表后必须产出全部 13 列，列名即手写升级脚本的依据。
    /// </summary>
    /// <remarks>
    /// 这些列名散落在 UpdateScripts 下的每一条手写 SQL 里，改名等于全部脚本失效，
    /// 而 CodeFirst 会照新名字建列、旧列留在库里，故障要到运行期才暴露。
    /// </remarks>
    [Fact]
    public void FullAuditedEntity_ShouldCreateSnakeCaseAuditColumns()
    {
        var columns = GetColumnNames(TestEntities.FullAuditedTableName);

        Assert.Contains("Basic_Id", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Row_Version", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Tenant_Id", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Created_Time", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Created_Id", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Created_By", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Modified_Time", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Modified_Id", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Modified_By", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Is_Deleted", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Deleted_Time", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Deleted_Id", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Deleted_By", columns, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 创建型实体建表后**不得**出现软删列，硬删约定在库层面成立。
    /// </summary>
    [Fact]
    public void CreationEntity_ShouldNotCreateSoftDeleteColumns()
    {
        var columns = GetColumnNames(TestEntities.CreationTableName);

        Assert.Contains("Created_Time", columns, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Is_Deleted", columns, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Deleted_Time", columns, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Modified_Time", columns, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 聚合根实体建表后列名是 PascalCase 而非 snake_case —— 锁定当前两套命名并存的实际差异。
    /// </summary>
    /// <remarks>
    /// 【缺陷锚点】框架 <c>SugarAggregateRoot</c> / <c>SugarMultiTenantAggregateRoot</c> 未给主键与审计列
    /// 指定 ColumnName，只有 RowVersion 例外。于是聚合根表得到 BasicId / TenantId / CreatedTime，
    /// 实体家族表得到 Basic_Id / Tenant_Id / Created_Time。今后针对聚合根表（SysUser、SysTenant、
    /// SysRole 等）手写 SQL 时若照 snake_case 直觉写，会报列不存在。
    /// <para>
    /// 一旦有人统一了命名，本断言变红——这是提醒，不是阻拦：请配套写列重命名升级脚本再改断言。
    /// </para>
    /// </remarks>
    [Fact]
    public void AggregateRoot_ShouldStillCreatePascalCaseColumns()
    {
        var columns = GetColumnNames(TestEntities.AggregateRootTableName);

        Assert.Contains("BasicId", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("TenantId", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("CreatedTime", columns, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("IsDeleted", columns, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Basic_Id", columns, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Tenant_Id", columns, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("Created_Time", columns, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 聚合根表的 Row_Version 仍是唯一一个显式命名的列。
    /// </summary>
    /// <remarks>
    /// 与上一条配套：它证明"聚合根表列名 PascalCase"不是全局格式化器造成的，
    /// 而确实来自逐列的 ColumnName 缺失。
    /// </remarks>
    [Fact]
    public void AggregateRoot_RowVersionShouldRemainSnakeCase()
    {
        var columns = GetColumnNames(TestEntities.AggregateRootTableName);

        Assert.Contains("Row_Version", columns, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 同租户同编码的未删除行只能存在一条。
    /// </summary>
    [Fact]
    public void UniqueIndex_ShouldRejectDuplicateCodeWithinSameTenant()
    {
        _ = _client.Insertable(CreateFullAudited(1L, 7L, "ORDER")).ExecuteCommand();

        _ = Assert.ThrowsAny<Exception>(
            () => _client.Insertable(CreateFullAudited(2L, 7L, "ORDER")).ExecuteCommand());
    }

    /// <summary>
    /// TenantId=0 的平台行与 TenantId=1 的租户行可以共存同一编码。
    /// </summary>
    /// <remarks>
    /// 这正是"平台记录用 TenantId = 0 而不是 NULL"这条口径带来的直接收益：
    /// 复合唯一索引因租户列非空而能正确区分两行。若 TenantId 可空，平台行之间会互相不"相等"，
    /// 唯一约束对全局记录失效。
    /// </remarks>
    [Fact]
    public void UniqueIndex_ShouldAllowSameCodeAcrossPlatformAndTenant()
    {
        var platformInserted = _client.Insertable(CreateFullAudited(10L, 0L, "SHARED")).ExecuteCommand();
        var tenantInserted = _client.Insertable(CreateFullAudited(11L, 1L, "SHARED")).ExecuteCommand();

        Assert.Equal(1, platformInserted);
        Assert.Equal(1, tenantInserted);
        Assert.Equal(2, _client.Queryable<CoreFullAuditedProbe>().Where(probe => probe.Code == "SHARED").Count());
    }

    /// <summary>
    /// 软删后可以重新创建同编码记录（唯一索引末列附加 IsDeleted 的意义所在）。
    /// </summary>
    [Fact]
    public void SoftDelete_ShouldAllowRecreatingSameCode()
    {
        var original = CreateFullAudited(20L, 3L, "REBUILD");
        _ = _client.Insertable(original).ExecuteCommand();

        original.IsDeleted = true;
        _ = _client.Updateable(original).ExecuteCommand();

        var recreated = _client.Insertable(CreateFullAudited(21L, 3L, "REBUILD")).ExecuteCommand();

        Assert.Equal(1, recreated);
        Assert.Equal(2, _client.Queryable<CoreFullAuditedProbe>().Where(probe => probe.Code == "REBUILD").Count());
    }

    /// <summary>
    /// 同一编码至多保留一条软删行：第二次软删同编码会撞唯一索引。
    /// </summary>
    /// <remarks>
    /// 这是源码注释里明写的限制——"如需第二次软删同编码记录，服务层须先物理清理旧软删行"。
    /// 把它写成断言，是为了让这条限制在实现变化时能被立刻发现，而不是等线上删除操作报唯一冲突。
    /// </remarks>
    [Fact]
    public void SoftDelete_SecondSoftDeleteOfSameCodeShouldHitUniqueIndex()
    {
        var first = CreateFullAudited(30L, 4L, "TWICE");
        _ = _client.Insertable(first).ExecuteCommand();
        first.IsDeleted = true;
        _ = _client.Updateable(first).ExecuteCommand();

        var second = CreateFullAudited(31L, 4L, "TWICE");
        _ = _client.Insertable(second).ExecuteCommand();
        second.IsDeleted = true;

        _ = Assert.ThrowsAny<Exception>(() => _client.Updateable(second).ExecuteCommand());
    }

    /// <summary>
    /// 插入的行必须能按 TenantId 与编码原样读回，租户列不会在往返中丢值。
    /// </summary>
    [Fact]
    public void Insert_ShouldPersistTenantIdAsWritten()
    {
        var entity = CreateFullAudited(40L, 99L, "ROUNDTRIP");
        _ = _client.Insertable(entity).ExecuteCommand();

        var saved = _client.Queryable<CoreFullAuditedProbe>().First(probe => probe.BasicId == 40L);

        Assert.Equal(99L, saved.TenantId);
        Assert.Equal("ROUNDTRIP", saved.Code, StringComparer.Ordinal);
        Assert.False(saved.IsDeleted);
    }

    /// <summary>
    /// 释放占用的资源并删除临时数据库文件。
    /// </summary>
    public void Dispose()
    {
        _client.Ado.Connection.Close();
        _client.Dispose();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }

    /// <summary>
    /// 读取指定表的实际列名集合。
    /// </summary>
    /// <param name="tableName">表名。</param>
    /// <returns>列名集合。</returns>
    private List<string> GetColumnNames(string tableName)
    {
        return [.. _client.DbMaintenance.GetColumnInfosByTableName(tableName, false).Select(column => column.DbColumnName)];
    }

    /// <summary>
    /// 构造完整审计样例实体并模拟 ORM 主键回填。
    /// </summary>
    /// <param name="basicId">主键。</param>
    /// <param name="tenantId">租户 Id（0 表示平台）。</param>
    /// <param name="code">业务编码。</param>
    /// <returns>样例实体。</returns>
    private static CoreFullAuditedProbe CreateFullAudited(long basicId, long tenantId, string code)
    {
        var entity = new CoreFullAuditedProbe(basicId)
        {
            TenantId = tenantId,
            Code = code,
            CreatedTime = DateTimeOffset.UnixEpoch
        };
        return entity;
    }
}
