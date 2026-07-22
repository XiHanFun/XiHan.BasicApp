// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 短信网关配置创建命令
/// </summary>
public sealed record SmsConfigCreateCommand(
    string ConfigCode,
    string ConfigName,
    SmsProviderType Provider,
    string AccessKeyId,
    string AccessKeySecret,
    string? SdkAppId,
    string SignName,
    string? Region,
    string? TemplateMap,
    bool IsDefault,
    bool IsEnabled,
    int Sort,
    string? Remark);

/// <summary>
/// 短信网关配置更新命令
/// </summary>
/// <remarks>AccessKeySecret 为空表示保留原密钥（前端脱敏不回显）</remarks>
public sealed record SmsConfigUpdateCommand(
    long BasicId,
    string ConfigName,
    SmsProviderType Provider,
    string AccessKeyId,
    string? AccessKeySecret,
    string? SdkAppId,
    string SignName,
    string? Region,
    string? TemplateMap,
    int Sort,
    string? Remark);

/// <summary>
/// 短信网关配置状态变更命令
/// </summary>
public sealed record SmsConfigStatusChangeCommand(long BasicId, bool IsEnabled);

/// <summary>
/// 短信网关配置默认变更命令
/// </summary>
public sealed record SmsConfigDefaultChangeCommand(long BasicId);

/// <summary>
/// 短信网关配置命令结果
/// </summary>
public sealed record SmsConfigCommandResult(SysSmsConfig Config);
