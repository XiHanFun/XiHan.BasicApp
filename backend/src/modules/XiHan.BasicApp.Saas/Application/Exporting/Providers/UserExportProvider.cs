#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UserExportProvider
// Guid:02f9c7e5-0b3d-4e8a-97c1-738495 06b7c8
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 用户导出 Provider（业务类型 system.user，复用 IUserQueryService 的分页 + 数据范围 + 字段脱敏）
/// </summary>
public sealed class UserExportProvider : QueryServiceExportProviderBase<UserPageQueryDto, UserListItemDto>
{
    private readonly IUserQueryService _userQueryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public UserExportProvider(IUserQueryService userQueryService)
    {
        _userQueryService = userQueryService;
    }

    /// <inheritdoc />
    public override string BusinessType => "system.user";

    /// <inheritdoc />
    public override string RequiredPermission => SaasPermissionCodes.User.Read;

    /// <inheritdoc />
    protected override Task<PageResultDtoBase<UserListItemDto>> QueryPageAsync(UserPageQueryDto query, CancellationToken cancellationToken)
    {
        return _userQueryService.GetUserPageAsync(query, cancellationToken);
    }
}
