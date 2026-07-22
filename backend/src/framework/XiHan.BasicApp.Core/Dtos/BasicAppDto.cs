// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
public class BasicAppPRDto : PageRequestDtoBase
{
}
