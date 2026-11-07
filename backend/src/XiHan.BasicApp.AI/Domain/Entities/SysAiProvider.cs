// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel.DataAnnotations;
using XiHan.BasicApp.Contracts.Entities;

namespace XiHan.BasicApp.AI.Domain.Entities;

/// <summary>
/// 模型提供商
/// </summary>
/// <remarks>
/// 平台库实体、只在平台一侧，租户经助手用到平台配置的提供商。只有 OpenAI 兼容一种协议，改端点接各家
/// 编码即框架 Provider 目录里的名字，建后不可改；API 密钥经 <c>ISecretProtector</c> 加密存储、响应里不回显
/// 超时接框架的 Provider 超时，最大输出与温度随每次聊天调用带上（方案 N4 D2）
/// </remarks>
[ActiveUniqueIndex(nameof(Code))]
public sealed class SysAiProvider : AppAuditedEntity
{
    /// <summary>
    /// 编码
    /// </summary>
    /// <remarks>
    /// 框架 Provider 名的文法：小写字母或数字开头，只有小写字母、数字与 <c>._-</c>，至多 64 个字符（方案 N4 D1）
    /// </remarks>
    [StringLength(64)]
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 名称
    /// </summary>
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 厂商
    /// </summary>
    /// <remarks>
    /// 只用于分组展示（OpenAI、DeepSeek 等，可自填），协议都是 OpenAI 兼容
    /// </remarks>
    [StringLength(50)]
    public string Vendor { get; set; } = string.Empty;

    /// <summary>
    /// 聊天模型
    /// </summary>
    [StringLength(100)]
    public string ChatModel { get; set; } = string.Empty;

    /// <summary>
    /// 嵌入模型
    /// </summary>
    /// <remarks>
    /// 为空即不能用于知识库
    /// </remarks>
    [StringLength(100)]
    public string? EmbeddingModel { get; set; }

    /// <summary>
    /// 端点
    /// </summary>
    /// <remarks>
    /// 绝对的 http 或 https 地址；厂商是 OpenAI 时可留空，走它的官方端点；别的厂商必填
    /// </remarks>
    [StringLength(500)]
    public string? Endpoint { get; set; }

    /// <summary>
    /// API 密钥（密文）
    /// </summary>
    [StringLength(2000)]
    public string? ApiKey { get; set; }

    /// <summary>
    /// 最大输出令牌数
    /// </summary>
    public int? MaxOutputTokens { get; set; }

    /// <summary>
    /// 采样温度
    /// </summary>
    /// <remarks>
    /// 0 到 2
    /// </remarks>
    public float? Temperature { get; set; }

    /// <summary>
    /// 单次调用超时（秒）
    /// </summary>
    /// <remarks>
    /// 为空即框架缺省的 100 秒；流式按每个增量计时
    /// </remarks>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// 是否缺省提供商
    /// </summary>
    /// <remarks>
    /// 最多一个；助手与知识库没写提供商时用它
    /// </remarks>
    public bool IsDefault { get; set; }

    /// <summary>
    /// 状态
    /// </summary>
    public AiStatus Status { get; set; } = AiStatus.Active;

    /// <summary>
    /// 排序号
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [StringLength(500)]
    public string? Remark { get; set; }
}
