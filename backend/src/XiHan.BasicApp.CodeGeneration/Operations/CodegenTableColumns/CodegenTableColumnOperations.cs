// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json;
using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Services;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.CodeGeneration.Operations.CodegenTableColumns;

/// <summary>
/// 一张表的列配置
/// </summary>
/// <remarks>
/// <c>GET /api/codegen-table-columns?tableId=</c>。按排序号；表配置不存在 <c>CODEGENERATION_NOTFOUND_002</c>
/// </remarks>
/// <param name="service">表配置规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TableColumnsRead)]
public sealed class ListCodegenTableColumns(CodeGenTableService service) : IOperation<ListCodegenTableColumnsRequest, CodegenTableColumnsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-table-columns.list";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTableColumnsResponse>> HandleAsync(ListCodegenTableColumnsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.TableId, context.CancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.Error!;
        }

        var columns = await service.ColumnsOfAsync(request.TableId, context.CancellationToken).ConfigureAwait(false);
        return new CodegenTableColumnsResponse([.. columns.Select(CodegenTableColumnResponse.From)]);
    }
}

/// <summary>
/// 整体保存一张表的列配置
/// </summary>
/// <remarks>
/// <c>POST /api/codegen-table-columns:save</c>。批量保存：每列都要属于这张表（<c>CODEGENERATION_VALIDATION_006</c>），交回读到的并发戳；
/// 选项来源只留它要的那个字段并校验必填（系统字典要字典码、枚举要全名、常量要合法 JSON、关联表要表与显示列、关联树要表），值变了的被跟踪字段记为人工改过
/// 任一列不过即整次失败、不写入
/// </remarks>
/// <param name="columns">列配置仓储</param>
/// <param name="service">表配置规则</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = CodeGenerationPermissions.TableColumnsUpdate)]
public sealed class SaveCodegenTableColumns(IRepository<SysCodeGenTableColumn, Guid> columns, CodeGenTableService service) : IOperation<SaveCodegenTableColumnsRequest, CodegenTableColumnsResponse>
{
    /// <summary>
    /// 操作 ID
    /// </summary>
    public const string Id = "codegen-table-columns.save";

    /// <inheritdoc />
    public async ValueTask<Result<CodegenTableColumnsResponse>> HandleAsync(SaveCodegenTableColumnsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        var found = await service.FindAsync(request.TableId, context.CancellationToken).ConfigureAwait(false);
        if (found.IsFailure)
        {
            return found.Error!;
        }

        var existing = (await service.ColumnsOfAsync(request.TableId, context.CancellationToken).ConfigureAwait(false)).ToDictionary(static c => c.Id);
        var changed = new List<SysCodeGenTableColumn>(request.Columns.Count);
        foreach (var input in request.Columns)
        {
            if (!existing.TryGetValue(input.Id, out var column))
            {
                return Error.Validation(CodeGenerationErrors.ColumnNotInTable, CodeGenerationMessages.ColumnNotInTable, MessageArguments.Of(("id", input.Id)));
            }

            var before = CodeGenModifiedFields.Snapshot(column, CodeGenTableService.TrackedColumnFields);
            var applied = Apply(column, input);
            if (applied.IsFailure)
            {
                return applied.Error!;
            }

            column.UserModifiedFields = CodeGenModifiedFields.Merge(column.UserModifiedFields, column, before);
            changed.Add(column);
        }

        foreach (var result in await columns.UpdateRangeAsync(changed, context.CancellationToken).ConfigureAwait(false))
        {
            if (result.IsFailure)
            {
                return result.Error!;
            }
        }

        var saved = await service.ColumnsOfAsync(request.TableId, context.CancellationToken).ConfigureAwait(false);
        return new CodegenTableColumnsResponse([.. saved.Select(CodegenTableColumnResponse.From)]);
    }

    private static Result Apply(SysCodeGenTableColumn column, CodegenTableColumnInput input)
    {
        column.ConcurrencyStamp = input.ConcurrencyStamp;
        column.CSharpType = CodeGenDataSourceService.Trimmed(input.CSharpType);
        column.CSharpProperty = CodeGenDataSourceService.Trimmed(input.CSharpProperty);
        column.TsType = CodeGenDataSourceService.Trimmed(input.TsType);
        column.IsRequired = input.IsRequired;
        column.IsUnique = input.IsUnique;
        column.IsList = input.IsList;
        column.IsInsert = input.IsInsert;
        column.IsEdit = input.IsEdit;
        column.IsQuery = input.IsQuery;
        column.QueryType = input.QueryType;
        column.HtmlType = input.HtmlType;
        column.DefaultValue = CodeGenDataSourceService.Trimmed(input.DefaultValue);
        column.MinValue = input.MinValue;
        column.MaxValue = input.MaxValue;
        column.RegexPattern = CodeGenDataSourceService.Trimmed(input.RegexPattern);
        column.ValidationMessage = CodeGenDataSourceService.Trimmed(input.ValidationMessage);
        column.Order = input.Order;
        column.Status = input.Status;
        return ApplySelector(column, input);
    }

    /// <summary>
    /// 套用选项来源
    /// </summary>
    /// <remarks>
    /// 只留当前来源要的那个字段并校验它必填（只校验生效的那个，免得前一种来源的残留值报出无关的错）
    /// </remarks>
    private static Result ApplySelector(SysCodeGenTableColumn column, CodegenTableColumnInput input)
    {
        column.DictCode = null;
        column.EnumTypeName = null;
        column.ConstValues = null;
        column.RelationTableId = null;
        column.RelationLabelColumn = null;
        column.DictSelectorType = input.DictSelectorType;
        switch (input.DictSelectorType)
        {
            case null:
                return Result.Success();
            case CodeGenDictSelectorType.DictSelector:
                column.DictCode = CodeGenDataSourceService.Trimmed(input.DictCode);
                return column.DictCode is null ? Invalid(column, CodeGenerationMessages.SelectorDictCodeRequired) : Result.Success();
            case CodeGenDictSelectorType.EnumSelector:
                column.EnumTypeName = CodeGenDataSourceService.Trimmed(input.EnumTypeName);
                return column.EnumTypeName is null ? Invalid(column, CodeGenerationMessages.SelectorEnumRequired) : Result.Success();
            case CodeGenDictSelectorType.ConstSelector:
                column.ConstValues = CodeGenDataSourceService.Trimmed(input.ConstValues);
                return column.ConstValues is null || !IsJson(column.ConstValues) ? Invalid(column, CodeGenerationMessages.SelectorConstInvalid) : Result.Success();
            case CodeGenDictSelectorType.TableSelector:
            case CodeGenDictSelectorType.TreeSelector:
                column.RelationTableId = input.RelationTableId;
                column.RelationLabelColumn = CodeGenDataSourceService.Trimmed(input.RelationLabelColumn);
                if (column.RelationTableId is null)
                {
                    return Invalid(column, CodeGenerationMessages.SelectorRelationRequired);
                }

                // 关联树缺省取目标树表的名称列，关联表没有这样的缺省
                return input.DictSelectorType == CodeGenDictSelectorType.TableSelector && column.RelationLabelColumn is null
                    ? Invalid(column, CodeGenerationMessages.SelectorLabelRequired)
                    : Result.Success();
            default:
                return Invalid(column, CodeGenerationMessages.SelectorDictCodeRequired);
        }
    }

    private static Error Invalid(SysCodeGenTableColumn column, string message)
        => Error.Validation(CodeGenerationErrors.SelectorInvalid, message, MessageArguments.Of(("column", column.ColumnName)));

    private static bool IsJson(string value)
    {
        try
        {
            using var document = JsonDocument.Parse(value);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
