// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.BasicApp.Tenancy.Operations.Tenants;

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户包操作的请求 / 响应序列化上下文（源生成；枚举按字符串）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(CreateTenantRequest))]
[JsonSerializable(typeof(GetTenantRequest))]
[JsonSerializable(typeof(UpdateTenantRequest))]
[JsonSerializable(typeof(TenantIdsRequest))]
[JsonSerializable(typeof(InitializeAdminRequest))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(TenantResponse))]
[JsonSerializable(typeof(TenantDetailResponse))]
[JsonSerializable(typeof(PageResult<TenantResponse>))]
[JsonSerializable(typeof(BatchResult))]
[JsonSerializable(typeof(UserResponse))]
public sealed partial class TenancyJsonContext : JsonSerializerContext;
