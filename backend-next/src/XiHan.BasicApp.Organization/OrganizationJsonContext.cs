// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Organization.Operations.Departments;
using XiHan.BasicApp.Organization.Operations.Memberships;
using XiHan.BasicApp.Organization.Operations.Positions;

namespace XiHan.BasicApp.Organization;

/// <summary>
/// 组织包操作的请求 / 响应序列化上下文（源生成；枚举按字符串）。
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(CreateDepartmentRequest))]
[JsonSerializable(typeof(GetDepartmentRequest))]
[JsonSerializable(typeof(DepartmentTreeRequest))]
[JsonSerializable(typeof(UpdateDepartmentRequest))]
[JsonSerializable(typeof(MoveDepartmentRequest))]
[JsonSerializable(typeof(DepartmentIdsRequest))]
[JsonSerializable(typeof(DepartmentResponse))]
[JsonSerializable(typeof(DepartmentNode))]
[JsonSerializable(typeof(DepartmentTreeResponse))]
[JsonSerializable(typeof(DepartmentDetailResponse))]
[JsonSerializable(typeof(CreatePositionRequest))]
[JsonSerializable(typeof(GetPositionRequest))]
[JsonSerializable(typeof(UpdatePositionRequest))]
[JsonSerializable(typeof(PositionIdsRequest))]
[JsonSerializable(typeof(PositionResponse))]
[JsonSerializable(typeof(PageResult<PositionResponse>))]
[JsonSerializable(typeof(CreateMembershipRequest))]
[JsonSerializable(typeof(UpdateMembershipRequest))]
[JsonSerializable(typeof(MembershipIdsRequest))]
[JsonSerializable(typeof(MembershipResponse))]
[JsonSerializable(typeof(PageResult<MembershipResponse>))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(BatchResult))]
public sealed partial class OrganizationJsonContext : JsonSerializerContext;
