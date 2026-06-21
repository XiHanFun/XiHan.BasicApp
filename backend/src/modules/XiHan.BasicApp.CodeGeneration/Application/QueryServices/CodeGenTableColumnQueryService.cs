#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableColumnQueryService
// Guid:c0de9e00-0803-4a00-9000-000000000803
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Application.Mappers;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.BasicApp.CodeGeneration.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.CodeGeneration.Application.QueryServices;

/// <summary>
/// 代码生成列配置查询应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "列配置")]
public sealed class CodeGenTableColumnQueryService : CodeGenerationApplicationService, ICodeGenTableColumnQueryService
{
    private readonly ICodeGenTableColumnRepository _columnRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTableColumnQueryService(ICodeGenTableColumnRepository columnRepository)
    {
        _columnRepository = columnRepository;
    }

    /// <inheritdoc />
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<IReadOnlyList<CodeGenTableColumnListItemDto>> GetByTableAsync(long tableId, CancellationToken cancellationToken = default)
    {
        if (tableId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tableId), "所属表主键必须大于 0。");
        }
        cancellationToken.ThrowIfCancellationRequested();

        var columns = await _columnRepository.GetByTableIdAsync(tableId, cancellationToken);
        return [.. columns.OrderBy(column => column.Sort).Select(CodeGenTableColumnApplicationMapper.ToListItemDto)];
    }
}
