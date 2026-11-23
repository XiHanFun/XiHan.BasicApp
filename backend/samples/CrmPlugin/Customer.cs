// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace CrmPlugin;

/// <summary>
/// 一个客户
/// </summary>
/// <param name="Id">客户 ID</param>
/// <param name="Name">名称</param>
/// <param name="Email">邮箱，可空</param>
internal sealed record Customer(Guid Id, string Name, string? Email);
