// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 用户导出 Provider（业务类型 identity.user，复用 IUserQueryService 的分页 + 数据范围 + 字段脱敏）
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

    /// <summary>
    /// 业务类型（= 导出按钮所属页面码，见 PageRegistry）
    /// </summary>
    public override string BusinessType => "identity.user";

    /// <summary>
    /// 导出所需权限码（与页面导出按钮 identity.user.export 绑定的权限一致；提交与执行都按它校验）
    /// </summary>
    public override string RequiredPermission => SaasPermissionCodes.User.Export;

    /// <summary>
    /// 调用对应 QueryService 的分页方法（子类实现）
    /// </summary>
    protected override Task<PageResultDtoBase<UserListItemDto>> QueryPageAsync(UserPageQueryDto query, CancellationToken cancellationToken)
    {
        return _userQueryService.GetUserPageAsync(query, cancellationToken);
    }
}
