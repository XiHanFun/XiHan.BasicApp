// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Printing.Application.Contracts;
using XiHan.BasicApp.Printing.Application.Dtos;
using XiHan.BasicApp.Printing.Domain.DataSources;
using XiHan.BasicApp.Printing.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.Permissions;
using XiHan.Framework.Core.Exceptions;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Printing.Application.QueryServices;

/// <summary>
/// 打印数据源目录查询 Dynamic API：设计器数据源下拉、字段素材与样例数据的单一来源。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Printing", GroupName = "打印服务", Tag = "打印数据源")]
public sealed class PrintDataSourceQueryService : PrintingApplicationService, IPrintDataSourceQueryService
{
    private readonly IPrintDataSourceRegistry _registry;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionChecker _permissionChecker;

    /// <summary>
    /// 初始化打印数据源查询服务。
    /// </summary>
    /// <param name="registry">数据源注册表。</param>
    /// <param name="currentUser">当前用户上下文。</param>
    /// <param name="permissionChecker">权限检查器。</param>
    public PrintDataSourceQueryService(
        IPrintDataSourceRegistry registry,
        ICurrentUser currentUser,
        IPermissionChecker permissionChecker)
    {
        _registry = registry;
        _currentUser = currentUser;
        _permissionChecker = permissionChecker;
    }

    /// <summary>
    /// 全部已注册的打印数据源（含字段清单与样例数据）。
    /// </summary>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>数据源目录。</returns>
    public async Task<List<PrintDataSourceDto>> GetListAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCatalogAccessAsync(cancellationToken);
        return [.. _registry.GetAll().Select(ToDto)];
    }

    /// <summary>
    /// 目录同时服务模板管理（read）与业务打印（use）两条路径，持有任一权限即可读取。
    /// </summary>
    private async Task EnsureCatalogAccessAsync(CancellationToken cancellationToken)
    {
        var userId = _currentUser.UserId ?? throw new UserFriendlyException("当前用户未登录。");
        var userIdText = userId.ToString();
        if (await _permissionChecker.IsGrantedAsync(userIdText, PrintingPermissionCodes.Read, cancellationToken)
            || await _permissionChecker.IsGrantedAsync(userIdText, PrintingPermissionCodes.Use, cancellationToken))
        {
            return;
        }

        throw new UserFriendlyException("缺少打印数据源目录访问权限。");
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
                    InputType = column.InputType,
                    Placeholder = column.Placeholder
                }).ToList()
            }).ToList()
        };
    }
}
