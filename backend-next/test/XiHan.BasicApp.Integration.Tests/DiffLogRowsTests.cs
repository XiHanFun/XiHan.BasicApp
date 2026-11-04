// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using SqlSugar;
using XiHan.BasicApp.Auditing.Domain.Entities;
using XiHan.BasicApp.Auditing.Domain.Services;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证差异事件转数据变更日志时的种类判定
/// </summary>
/// <remarks>
/// 软删除列由真变假即恢复（4.x 同），由假变真与其余修改仍是修改；各库读回的布尔类型不同，都认
/// </remarks>
public sealed class DiffLogRowsTests
{
    private static DiffLogTableInfo Row(string id, object? deleted, string name) => new()
    {
        TableName = "sys_config",
        Columns =
        [
            new DiffLogColumnInfo { ColumnName = "id", Value = id, IsPrimaryKey = true },
            new DiffLogColumnInfo { ColumnName = "name", Value = name },
            new DiffLogColumnInfo { ColumnName = "is_deleted", Value = deleted },
        ],
    };

    private static List<SysDiffLog> Convert(DiffLogTableInfo before, DiffLogTableInfo after)
        => DiffLogRows.From(new DiffLogModel { DiffType = DiffType.update, BeforeData = [before], AfterData = [after] }, "configs.update").ToList();

    [Theory]
    [InlineData(true, false)]
    [InlineData(1L, 0L)]
    [InlineData("True", "False")]
    public void UndeletingASoftDeletedRow_IsARestore(object before, object after)
    {
        var log = Assert.Single(Convert(Row("a", before, "Limit"), Row("a", after, "Limit")));
        Assert.Equal(DiffKind.Restore, log.Kind);
        Assert.Equal("is_deleted", log.ChangedFields);
    }

    [Fact]
    public void Secrets_NeverReachTheLog()
    {
        static DiffLogTableInfo Config(bool encrypted, string value) => new()
        {
            TableName = "sys_config",
            Columns =
            [
                new DiffLogColumnInfo { ColumnName = "id", Value = "c", IsPrimaryKey = true },
                new DiffLogColumnInfo { ColumnName = "is_encrypted", Value = encrypted },
                new DiffLogColumnInfo { ColumnName = "value", Value = value },
            ],
        };

        // 加密参数的值，改之前或改之后是加密的都不留
        var change = Assert.Single(Convert(Config(true, "dp:cipher"), Config(true, "dp:other")));
        Assert.DoesNotContain("dp:", change.Before!, StringComparison.Ordinal);
        Assert.DoesNotContain("dp:", change.After!, StringComparison.Ordinal);
        Assert.Contains("plain", Assert.Single(Convert(Config(false, "plain"), Config(false, "plain2"))).Before!, StringComparison.Ordinal);

        // 带凭据的列按名字遮
        foreach (var column in new[] { "webhook_url", "api_key", "connection_string", "password", "client_secret_hash" })
        {
            var row = new DiffLogTableInfo { TableName = "t", Columns = [new DiffLogColumnInfo { ColumnName = "id", Value = "a", IsPrimaryKey = true }, new DiffLogColumnInfo { ColumnName = column, Value = "credential" }] };
            var other = new DiffLogTableInfo { TableName = "t", Columns = [new DiffLogColumnInfo { ColumnName = "id", Value = "a", IsPrimaryKey = true }, new DiffLogColumnInfo { ColumnName = column, Value = "credential2" }] };
            Assert.DoesNotContain("credential", Assert.Single(Convert(row, other)).After!, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SoftDeletingAndOtherEdits_StayUpdates()
    {
        Assert.Equal(DiffKind.Update, Assert.Single(Convert(Row("a", false, "Limit"), Row("a", true, "Limit"))).Kind);
        Assert.Equal(DiffKind.Update, Assert.Single(Convert(Row("a", false, "Limit"), Row("a", false, "Cap"))).Kind);
        var withoutColumn = new DiffLogTableInfo { TableName = "sys_job", Columns = [new DiffLogColumnInfo { ColumnName = "id", Value = "j", IsPrimaryKey = true }, new DiffLogColumnInfo { ColumnName = "name", Value = "x" }] };
        var renamed = new DiffLogTableInfo { TableName = "sys_job", Columns = [new DiffLogColumnInfo { ColumnName = "id", Value = "j", IsPrimaryKey = true }, new DiffLogColumnInfo { ColumnName = "name", Value = "y" }] };
        Assert.Equal(DiffKind.Update, Assert.Single(Convert(withoutColumn, renamed)).Kind);
    }
}
