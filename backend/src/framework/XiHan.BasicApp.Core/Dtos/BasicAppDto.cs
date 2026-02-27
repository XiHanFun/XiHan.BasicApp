#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:BasicAppDto
// Guid:892d867f-e909-4e3c-80c9-81c2445e07dc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2025/10/10 06:33:01
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.Framework.Application.Contracts.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Core.Dtos;

/// <summary>
/// BasicApp DTO 基类
/// 命名规则：Dto（Data Transfer Object）
/// </summary>
public abstract class BasicAppDto : DtoBase<long>
{
}

/// <summary>
/// BasicApp 创建 DTO 基类
/// 命名规则：CDto（Create DTO）
/// </summary>
public abstract class BasicAppCDto : CreationDtoBase<long>
{
}

/// <summary>
/// BasicApp 修改 DTO 基类
/// 命名规则：UDto（Update DTO）
/// </summary>
public abstract class BasicAppUDto : UpdateDtoBase<long>
{
}

/// <summary>
/// BasicApp 分页请求 DTO 基类
/// 命名规则：PRDto（Page Request DTO）
/// </summary>
public abstract class BasicAppPRDto : PageRequestDtoBase
{
}
