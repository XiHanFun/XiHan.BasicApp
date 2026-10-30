// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity.Operations.Users;
using XiHan.BasicApp.Tenancy.Operations.Editions;
using XiHan.BasicApp.Tenancy.Operations.Subscription;
using XiHan.BasicApp.Tenancy.Operations.TenantMembers;
using XiHan.BasicApp.Tenancy.Operations.Tenants;

namespace XiHan.BasicApp.Tenancy;

/// <summary>
/// 租户包操作的请求 / 响应序列化上下文。
/// </summary>
/// <remarks>
/// 源生成；枚举按字符串。
/// </remarks>
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
[JsonSerializable(typeof(TransferOwnerRequest))]
[JsonSerializable(typeof(ConfigureTenantDatabaseRequest))]
[JsonSerializable(typeof(InitializeTenantDatabaseRequest))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(TenantResponse))]
[JsonSerializable(typeof(TenantDetailResponse))]
[JsonSerializable(typeof(TenantQuotaResponse))]
[JsonSerializable(typeof(ListTenantQuotasRequest))]
[JsonSerializable(typeof(TenantQuotasResponse))]
[JsonSerializable(typeof(GetSubscriptionRequest))]
[JsonSerializable(typeof(SubscriptionResponse))]
[JsonSerializable(typeof(PageResult<TenantResponse>))]
[JsonSerializable(typeof(BatchResult))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(MyTenantsRequest))]
[JsonSerializable(typeof(MyTenantsResponse))]
[JsonSerializable(typeof(AvailableTenantResponse))]
[JsonSerializable(typeof(TenantMembersRequest))]
[JsonSerializable(typeof(TenantMembersResponse))]
[JsonSerializable(typeof(TenantMemberResponse))]
[JsonSerializable(typeof(AddTenantMemberRequest))]
[JsonSerializable(typeof(TenantMemberIdsRequest))]
[JsonSerializable(typeof(ChangeTenantEditionRequest))]
[JsonSerializable(typeof(ListEditionsRequest))]
[JsonSerializable(typeof(GetEditionRequest))]
[JsonSerializable(typeof(CreateEditionRequest))]
[JsonSerializable(typeof(UpdateEditionRequest))]
[JsonSerializable(typeof(SetEditionPermissionsRequest))]
[JsonSerializable(typeof(EditionIdsRequest))]
[JsonSerializable(typeof(EditionResponse))]
[JsonSerializable(typeof(EditionsResponse))]
[JsonSerializable(typeof(EditionDetailResponse))]
public sealed partial class TenancyJsonContext : JsonSerializerContext;
