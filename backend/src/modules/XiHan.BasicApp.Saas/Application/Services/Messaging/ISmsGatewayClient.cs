#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ISmsGatewayClient
// Guid:3e6a8c15-9d40-4b72-a1f8-5c2d7e9b0a36
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 15:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Services;

/// <summary>
/// 短信网关客户端（绑定一条 SysSmsConfig 配置，由 <see cref="ISmsGatewayResolver"/> 按指纹构建缓存）
/// </summary>
public interface ISmsGatewayClient
{
    /// <summary>
    /// 服务商类型
    /// </summary>
    SmsProviderType Provider { get; }

    /// <summary>
    /// 发送短信
    /// </summary>
    /// <param name="request">发送请求（内部模板码，由客户端按配置的模板映射转换为服务商模板码）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>发送结果</returns>
    Task<SmsGatewaySendResult> SendAsync(SmsGatewayRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// 短信网关发送请求
/// </summary>
/// <param name="PhoneNumbers">手机号列表（已按逗号拆分）</param>
/// <param name="TemplateCode">内部模板码（SysMessageTemplate 编码，经配置 TemplateMap 映射为服务商模板码）</param>
/// <param name="TemplateParamsJson">模板参数 JSON（键值对；键须与服务商模板变量名一致）</param>
/// <param name="Content">已渲染内容（仅审计/日志参考，云厂商按模板发送不使用）</param>
public sealed record SmsGatewayRequest(
    IReadOnlyList<string> PhoneNumbers,
    string? TemplateCode,
    string? TemplateParamsJson,
    string Content);

/// <summary>
/// 短信网关发送结果
/// </summary>
/// <param name="IsSuccess">是否全部成功（多号发送任一失败即失败）</param>
/// <param name="ProviderMessageId">服务商回执ID（阿里云 BizId / 腾讯云 SerialNo，多号逗号分隔）</param>
/// <param name="ErrorMessage">失败信息</param>
public sealed record SmsGatewaySendResult(
    bool IsSuccess,
    string? ProviderMessageId,
    string? ErrorMessage);
