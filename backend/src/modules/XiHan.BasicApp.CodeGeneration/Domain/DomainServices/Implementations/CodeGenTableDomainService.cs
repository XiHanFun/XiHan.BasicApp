#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableDomainService
// Guid:c0de9e00-0602-4a00-9000-000000000d04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.CodeGeneration.Domain.Generation;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成表配置领域服务实现
/// </summary>
public sealed class CodeGenTableDomainService : ICodeGenTableDomainService
{
    private readonly ICodeGenTableRepository _tableRepository;

    private readonly ICodeGenTableColumnRepository _columnRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTableDomainService(
        ICodeGenTableRepository tableRepository,
        ICodeGenTableColumnRepository columnRepository)
    {
        _tableRepository = tableRepository;
        _columnRepository = columnRepository;
    }

    /// <inheritdoc />
    public async Task<CodeGenTableCommandResult> UpdateTableAsync(CodeGenTableUpdateCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "表配置主键必须大于 0。");
        ValidateEnum(command.TemplateType, nameof(command.TemplateType));
        ValidateEnum(command.GenType, nameof(command.GenType));
        ValidateEnum(command.DatabaseType, nameof(command.DatabaseType));
        ValidateEnum(command.Status, nameof(command.Status));

        var tableName = Required(command.TableName, 200, nameof(command.TableName), "数据库表名不能为空。");
        var className = Required(command.ClassName, 200, nameof(command.ClassName), "实体类名称不能为空。");

        // 表名唯一（排除自身）
        if (await _tableRepository.ExistsTableNameAsync(tableName, command.BasicId, cancellationToken))
        {
            throw new InvalidOperationException("数据库表名已配置。");
        }

        var table = await GetTableOrThrowAsync(command.BasicId, cancellationToken);

        // dirty-tracking：应用前快照，应用后 diff，把变化的字段并入"已人工修改"集合
        var snapshot = UserModifiedFieldSet.Snapshot(table, TrackedTableFields);

        table.TableName = tableName;
        table.TableComment = Optional(command.TableComment, 500, nameof(command.TableComment), "表描述长度不能超过 500 个字符。");
        table.ClassName = className;
        table.Namespace = Optional(command.Namespace, 500, nameof(command.Namespace), "命名空间长度不能超过 500 个字符。");
        table.ModuleName = Optional(command.ModuleName, 100, nameof(command.ModuleName), "模块名称长度不能超过 100 个字符。");
        table.BusinessName = Optional(command.BusinessName, 100, nameof(command.BusinessName), "业务名称长度不能超过 100 个字符。");
        table.FunctionName = Optional(command.FunctionName, 100, nameof(command.FunctionName), "功能名称长度不能超过 100 个字符。");
        table.Author = Optional(command.Author, 100, nameof(command.Author), "作者长度不能超过 100 个字符。");
        table.TemplateType = command.TemplateType;
        table.GenType = command.GenType;
        table.GenPath = Optional(command.GenPath, 500, nameof(command.GenPath), "生成路径长度不能超过 500 个字符。");
        table.ParentMenuId = command.ParentMenuId;
        table.PrimaryKeyColumn = Optional(command.PrimaryKeyColumn, 100, nameof(command.PrimaryKeyColumn), "主键列名长度不能超过 100 个字符。");
        table.TreeParentColumn = Optional(command.TreeParentColumn, 100, nameof(command.TreeParentColumn), "树表父级字段长度不能超过 100 个字符。");
        table.TreeNameColumn = Optional(command.TreeNameColumn, 100, nameof(command.TreeNameColumn), "树表名称字段长度不能超过 100 个字符。");
        table.MasterTableId = command.MasterTableId;
        table.MasterForeignKey = Optional(command.MasterForeignKey, 100, nameof(command.MasterForeignKey), "主子表关联外键列长度不能超过 100 个字符。");
        table.DatabaseType = command.DatabaseType;
        table.DataSourceId = command.DataSourceId;
        table.Options = NormalizeNullable(command.Options);
        table.Status = command.Status;
        table.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注长度不能超过 500 个字符。");

        var changed = UserModifiedFieldSet.DiffChanged(table, snapshot);
        table.UserModifiedFields = UserModifiedFieldSet.Merge(table.UserModifiedFields, changed);

        return new CodeGenTableCommandResult(await _tableRepository.UpdateAsync(table, cancellationToken));
    }

    /// <summary>
    /// 参与 dirty-tracking 的表字段（会被同步表结构重新推断覆盖的字段）
    /// </summary>
    private static readonly string[] TrackedTableFields =
    [
        nameof(SysCodeGenTable.ClassName),
        nameof(SysCodeGenTable.Namespace),
        nameof(SysCodeGenTable.ModuleName),
        nameof(SysCodeGenTable.BusinessName),
        nameof(SysCodeGenTable.FunctionName),
        nameof(SysCodeGenTable.Author),
        nameof(SysCodeGenTable.TemplateType),
        nameof(SysCodeGenTable.TreeParentColumn),
        nameof(SysCodeGenTable.TreeNameColumn)
    ];

    /// <inheritdoc />
    public async Task<CodeGenTableCommandResult> UpdateTableStatusAsync(CodeGenTableStatusChangeCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        cancellationToken.ThrowIfCancellationRequested();

        EnsureId(command.BasicId, "表配置主键必须大于 0。");
        ValidateEnum(command.Status, nameof(command.Status));

        var table = await GetTableOrThrowAsync(command.BasicId, cancellationToken);

        table.Status = command.Status;
        table.Remark = Optional(command.Remark, 500, nameof(command.Remark), "备注长度不能超过 500 个字符。");

        return new CodeGenTableCommandResult(await _tableRepository.UpdateAsync(table, cancellationToken));
    }

    /// <inheritdoc />
    public async Task DeleteTableAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var table = await GetTableOrThrowAsync(id, cancellationToken);

        // 先级联软删列配置，再软删表本身
        await _columnRepository.DeleteByTableIdAsync(table.BasicId, cancellationToken);

        if (!await _tableRepository.DeleteAsync(table, cancellationToken))
        {
            throw new InvalidOperationException("表配置删除失败。");
        }
    }

    private static void EnsureId(long id, string message)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), message);
        }
    }

    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, "枚举值无效。");
        }
    }

    private static string Required(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException(message, paramName);
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(message, paramName);
        }

        return trimmed;
    }

    private static string? Optional(string? value, int maxLength, string paramName, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
        {
            throw new ArgumentException(message, paramName);
        }

        return trimmed;
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private async Task<SysCodeGenTable> GetTableOrThrowAsync(long id, CancellationToken cancellationToken)
    {
        EnsureId(id, "表配置主键必须大于 0。");
        return await _tableRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException("表配置不存在。");
    }
}
