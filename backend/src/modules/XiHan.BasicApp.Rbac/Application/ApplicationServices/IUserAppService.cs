#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IUserAppService
// Guid:0959ada9-f97b-40bb-a865-146a8b3658c9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/28 05:46:27
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Rbac.Application.Dtos;
using XiHan.BasicApp.Core.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Rbac.Application.ApplicationServices;

/// <summary>
/// 用户应用服务
/// </summary>
public interface IUserAppService
    : ICrudApplicationService<UserDto, long, UserCreateDto, UserUpdateDto, BasicAppPRDto>
{
    /// <summary>
    /// 根据用户名获取用户
    /// </summary>
    /// <param name="userName"></param>
    /// <param name="tenantId"></param>
    /// <returns></returns>
    Task<UserDto?> GetByUserNameAsync(string userName, long? tenantId = null);

}
