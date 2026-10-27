// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity.Operations.Users;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 本包全部请求 / 响应 DTO 的源生成序列化上下文：操作登记、HTTP 载荷绑定与响应编码都用它，不走反射。
/// camelCase 属性名、不区分大小写、允许从字符串读数字（路由值与查询串都是字符串）、枚举写成字符串（前端得到字面量联合类型）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(CreateUserRequest))]
[JsonSerializable(typeof(GetUserRequest))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(PageResult<UserResponse>))]
public sealed partial class IdentityJsonContext : JsonSerializerContext;
