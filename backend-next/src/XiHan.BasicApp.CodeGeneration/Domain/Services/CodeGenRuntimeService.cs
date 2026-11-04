// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Buffers;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SqlSugar;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Operations.CodegenRuntime;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Runtime.Persistence;
using XiHan.Framework.Persistence.Abstractions.Schema;
using XiHan.Framework.Persistence.SqlSugar;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Domain.Services;

/// <summary>
/// 零代码运行时：按表配置只读查看数据
/// </summary>
/// <remarks>
/// 照 4.x 的 <c>DynamicRuntimeAppService</c>：表名只取已配置且启用的表配置（不接受请求给的表名，没有注入面），按列配置给出表结构，按表名分页只读，不生成也不编译代码
/// 主库的表照 4.x 补上与实体读同口径的条件：有租户列的只看调用方这一侧的行，有软删列的不看已删的
/// 与 4.x 不同（按缺陷修，方案 C2）：按表配置的数据源查（4.x 一律查主库）；外部库的表不套这两个条件（租户与软删是 BasicApp 表的约定）；
/// 只按列配置逐列取（4.x 取 *），数据行按属性名（camelCase）给键（4.x 按库里的列名给键，前端按属性名取，取不到）；列配置里有、库里已没有的列读不出，先同步结构
/// </remarks>
/// <param name="tables">表配置服务</param>
/// <param name="dataSources">数据源服务</param>
/// <param name="unitOfWork">工作单元</param>
/// <param name="entities">已登记的实体（应用自己的表）</param>
public sealed partial class CodeGenRuntimeService(CodeGenTableService tables, CodeGenDataSourceService dataSources, SqlSugarUnitOfWork unitOfWork, IEntityCatalog entities)
{
    [GeneratedRegex(@"^[\p{L}\p{N}_$][\p{L}\p{N}_$ .-]*$")]
    private static partial Regex SafeTableName();

    /// <summary>
    /// 表是不是这个实体的
    /// </summary>
    /// <remarks>
    /// 分表登记的是逻辑名（如 <c>sys_access_log_{year}{month}{day}</c>），物理表按占位符之前的前缀认
    /// </remarks>
    private static bool IsTableOf(EntityMapping entity, string table)
    {
        var placeholder = entity.Table.IndexOf('{', StringComparison.Ordinal);
        return placeholder < 0
            ? string.Equals(entity.Table, table, StringComparison.OrdinalIgnoreCase)
            : table.StartsWith(entity.Table[..placeholder], StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 表结构
    /// </summary>
    /// <param name="id">表配置 ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task<Result<CodegenRuntimeSchemaResponse>> SchemaAsync(Guid id, CancellationToken cancellationToken)
    {
        var enabled = await EnabledAsync(id, cancellationToken).ConfigureAwait(false);
        if (!enabled.TryGetValue(out var table))
        {
            return enabled.Error!;
        }

        var columns = await tables.ColumnsOfAsync(table.Id, cancellationToken).ConfigureAwait(false);
        return new CodegenRuntimeSchemaResponse(table.Id, table.TableName, table.ClassName, table.TableComment, [.. columns.Select(static c => new CodegenRuntimeColumnResponse(
            c.ColumnName,
            PropertyOf(c),
            c.ColumnComment,
            c.TsType,
            c.HtmlType,
            c.QueryType,
            c.IsList,
            c.IsQuery,
            c.IsRequired))]);
    }

    /// <summary>
    /// 分页只读数据
    /// </summary>
    /// <param name="request">请求</param>
    /// <param name="context">调用上下文</param>
    public async Task<Result<PageResult<IReadOnlyDictionary<string, JsonElement>>>> ListAsync(ListCodegenRuntimeRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var enabled = await EnabledAsync(request.Id, context.CancellationToken).ConfigureAwait(false);
        if (!enabled.TryGetValue(out var table))
        {
            return enabled.Error!;
        }

        var columns = await tables.ColumnsOfAsync(table.Id, context.CancellationToken).ConfigureAwait(false);
        ISqlSugarClient client;
        if (table.DataSourceId is { } sourceId)
        {
            var connected = await dataSources.ConnectAsync(sourceId, context.CancellationToken).ConfigureAwait(false);
            if (!connected.TryGetValue(out var source))
            {
                return connected.Error!;
            }

            client = ((SqlSugarDataSource)source.Connection).Client;
        }
        else
        {
            client = unitOfWork.ConnectionFor(typeof(SysCodeGenTable));
        }

        if (columns.Count == 0)
        {
            return new PageResult<IReadOnlyDictionary<string, JsonElement>>([], 0, request.Page);
        }

        // 表名进 AS：SqlSugar 见到括号或引号就原样拼进 SQL，这里只放行表名该有的字符
        if (!SafeTableName().IsMatch(table.TableName))
        {
            return Error.Validation(CodeGenerationErrors.RuntimeTableReserved, CodeGenerationMessages.RuntimeTableReserved, MessageArguments.Of(("table", table.TableName)));
        }

        // 主库里应用自己的表（账号、口令散列、密钥）不经零代码运行时读
        if (table.DataSourceId is null && (await entities.ListAsync(context.CancellationToken).ConfigureAwait(false)).Any(e => IsTableOf(e, table.TableName)))
        {
            return Error.Validation(CodeGenerationErrors.RuntimeTableReserved, CodeGenerationMessages.RuntimeTableReserved, MessageArguments.Of(("table", table.TableName)));
        }

        // 按列配置逐列取（Oracle 的分页不接受 * 与行号并列），列名照原样加引号
        var query = client.Queryable<object>().AS(table.TableName);
        var builder = query.QueryBuilder.Builder;
        query = query.Select(string.Join(", ", columns.Select(c => builder.GetTranslationColumnName(c.ColumnName))));
        if (table.DataSourceId is null)
        {
            // 原始表不经实体读的过滤：租户与软删的条件按实体读同口径显式补上（照 4.x）
            if (Find(columns, "tenant_id", "tenantid") is { } tenant)
            {
                query = query.Where($"{builder.GetTranslationColumnName(tenant)} = @tenantId", new { tenantId = SideRows.SideOf(context) });
            }

            if (Find(columns, "is_deleted", "isdeleted") is { } deleted)
            {
                query = query.Where($"{builder.GetTranslationColumnName(deleted)} = @isDeleted", new { isDeleted = false });
            }
        }

        DataTable data;
        var total = new RefAsync<int>();
        try
        {
            data = await query.ToDataTablePageAsync(request.Page.Index, request.Page.Size, total).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is SqlSugarException or DbException)
        {
            return Error.Unavailable(CodeGenerationErrors.RuntimeReadFailed, CodeGenerationMessages.RuntimeReadFailed, MessageArguments.Of(("table", table.TableName), ("reason", exception.Message)));
        }

        var keys = columns
            .Select(static c => (Column: c.ColumnName, Key: PropertyOf(c)))
            .Where(c => data.Columns.Contains(c.Column))
            .ToList();
        var buffer = new ArrayBufferWriter<byte>();
        using var writer = new Utf8JsonWriter(buffer);
        var rows = new List<IReadOnlyDictionary<string, JsonElement>>(data.Rows.Count);
        foreach (DataRow row in data.Rows)
        {
            var item = new Dictionary<string, JsonElement>(keys.Count, StringComparer.Ordinal);
            foreach (var (column, key) in keys)
            {
                item[key] = ElementOf(row[column], buffer, writer);
            }

            rows.Add(item);
        }

        return new PageResult<IReadOnlyDictionary<string, JsonElement>>(rows, total.Value, request.Page);
    }

    // 已配置且启用的表配置（照 4.x，停用的不能访问）
    private async Task<Result<SysCodeGenTable>> EnabledAsync(Guid id, CancellationToken cancellationToken)
    {
        var found = await tables.FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (!found.TryGetValue(out var table))
        {
            return found.Error!;
        }

        return table.Status == CodeGenStatus.Active
            ? table
            : Error.Conflict(CodeGenerationErrors.TableDisabled, CodeGenerationMessages.RuntimeTableDisabled, MessageArguments.Of(("name", table.TableName)));
    }

    private static string PropertyOf(SysCodeGenTableColumn column)
        => CodeGenNaming.Camelize(CodeGenDataSourceService.Trimmed(column.CSharpProperty) ?? CodeGenNaming.Pascalize(column.ColumnName));

    private static string? Find(IReadOnlyList<SysCodeGenTableColumn> columns, params string[] names)
        => columns.Select(static c => c.ColumnName).FirstOrDefault(name => names.Contains(name, StringComparer.OrdinalIgnoreCase));

    // 单元格 → JSON：数字、布尔、时间与 Guid 按原类型，非有限的浮点、二进制（Base64）与其余按文本
    private static JsonElement ElementOf(object? value, ArrayBufferWriter<byte> buffer, Utf8JsonWriter writer)
    {
        buffer.ResetWrittenCount();
        writer.Reset(buffer);
        switch (value)
        {
            case null or DBNull:
                writer.WriteNullValue();
                break;
            case string text:
                writer.WriteStringValue(text);
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case byte or sbyte or short or ushort or int or uint or long:
                writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
                break;
            case decimal number:
                writer.WriteNumberValue(number);
                break;
            case double number when double.IsFinite(number):
                writer.WriteNumberValue(number);
                break;
            case float number when float.IsFinite(number):
                writer.WriteNumberValue(number);
                break;
            case DateTime time:
                writer.WriteStringValue(time);
                break;
            case DateTimeOffset time:
                writer.WriteStringValue(time);
                break;
            case Guid guid:
                writer.WriteStringValue(guid);
                break;
            case TimeSpan span:
                writer.WriteStringValue(span.ToString("c", CultureInfo.InvariantCulture));
                break;
            case byte[] bytes:
                writer.WriteBase64StringValue(bytes);
                break;
            default:
                writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }

        writer.Flush();
        var reader = new Utf8JsonReader(buffer.WrittenSpan);
        return JsonElement.ParseValue(ref reader);
    }
}
