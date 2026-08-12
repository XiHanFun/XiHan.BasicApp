// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Printing.Application.Contracts;
using XiHan.BasicApp.Printing.Application.Dtos;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;

namespace XiHan.BasicApp.Printing.Application.QueryServices;

/// <summary>
/// 打印数据源目录查询 Dynamic API：设计器数据源下拉、字段素材与样例数据的单一来源。
/// </summary>
[DynamicApi(Group = "BasicApp.Printing", GroupName = "打印服务", Tag = "打印数据源")]
public sealed class PrintDataSourceQueryService : PrintingApplicationService, IPrintDataSourceQueryService
{
    private readonly IPrintDataSourceRegistry _registry;

    /// <summary>
    /// 初始化打印数据源查询服务。
    /// </summary>
    /// <param name="registry">数据源注册表。</param>
    public PrintDataSourceQueryService(IPrintDataSourceRegistry registry)
    {
        _registry = registry;
    }

    /// <inheritdoc />
    [PermissionAuthorize(PrintingPermissionCodes.Read)]
    public Task<List<PrintDataSourceDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var result = _registry.GetAll().Select(ToDto).ToList();
        return Task.FromResult(result);
    }

    private static PrintDataSourceDto ToDto(PrintDataSourceDefinition definition)
    {
        return new PrintDataSourceDto
        {
            Code = definition.Code,
            Name = definition.Name,
            SampleDataJson = definition.SampleDataJson,
            Fields = definition.Fields.Select(field => new PrintDataSourceFieldDto
            {
                Key = field.Key,
                Label = field.Label,
                Kind = field.Kind,
                InputType = field.InputType,
                Placeholder = field.Placeholder,
                Columns = field.Columns?.Select(column => new PrintDataSourceTableColumnDto
                {
                    Field = column.Field,
                    Title = column.Title,
                    Width = column.Width,
                    InputType = column.InputType
                }).ToList()
            }).ToList()
        };
    }
}
