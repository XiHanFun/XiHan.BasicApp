// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Text.Json.Serialization;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.BasicApp.Contracts.Paging;
using XiHan.BasicApp.Identity.Operations.Auth;
using XiHan.BasicApp.Identity.Operations.Impersonation;
using XiHan.BasicApp.Identity.Operations.Profile;
using XiHan.BasicApp.Identity.Operations.UserSettings;
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
[JsonSerializable(typeof(UpdateUserRequest))]
[JsonSerializable(typeof(UserIdsRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(PageRequest))]
[JsonSerializable(typeof(UserResponse))]
[JsonSerializable(typeof(PageResult<UserResponse>))]
[JsonSerializable(typeof(BatchResult))]
[JsonSerializable(typeof(PasswordChangedResponse))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(LogoutRequest))]
[JsonSerializable(typeof(LogoutResponse))]
[JsonSerializable(typeof(MeRequest))]
[JsonSerializable(typeof(MeResponse))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(ChangePasswordRequest))]
[JsonSerializable(typeof(GetUserSettingRequest))]
[JsonSerializable(typeof(UpdateUserSettingRequest))]
[JsonSerializable(typeof(UserSettingResponse))]
[JsonSerializable(typeof(SwitchTenantRequest))]
[JsonSerializable(typeof(ActivateRolesRequest))]
[JsonSerializable(typeof(StartImpersonationRequest))]
[JsonSerializable(typeof(StopImpersonationRequest))]
[JsonSerializable(typeof(ImpersonationCandidatesRequest))]
[JsonSerializable(typeof(ImpersonationCandidateResponse))]
[JsonSerializable(typeof(ImpersonationCandidatesResponse))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(VerifyTwoFactorRequest))]
[JsonSerializable(typeof(CaptchaRequest))]
[JsonSerializable(typeof(CaptchaResponse))]
[JsonSerializable(typeof(LoginConfigRequest))]
[JsonSerializable(typeof(LoginConfigResponse))]
[JsonSerializable(typeof(ProfileRequest))]
[JsonSerializable(typeof(ProfileResponse))]
[JsonSerializable(typeof(TotpSetupResponse))]
[JsonSerializable(typeof(TwoFactorChangeRequest))]
public sealed partial class IdentityJsonContext : JsonSerializerContext;
