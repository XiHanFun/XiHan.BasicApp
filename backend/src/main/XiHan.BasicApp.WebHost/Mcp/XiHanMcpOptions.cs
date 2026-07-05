#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanMcpOptions
// Guid:a11c0de0-8001-4a10-9a00-00000000ai80
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 18:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.WebHost.Mcp;

/// <summary>
/// MCP Server 配置（appsettings 的 XiHan:AI:Mcp 节；属部署级基础设施配置）
/// </summary>
/// <remarks>
/// 鉴权为「应用管理的 key」：请求须带 <see cref="HeaderName"/>(或 Authorization: Bearer)且值等于 <see cref="ApiKey"/>。
/// fail-closed：未开启或未配置 ApiKey 则不暴露 /mcp 端点。
/// </remarks>
public sealed class XiHanMcpOptions
{
    /// <summary>
    /// 配置节名
    /// </summary>
    public const string SectionName = "XiHan:AI:Mcp";

    /// <summary>
    /// 是否启用 MCP Server（默认关；须显式开启并配置 ApiKey 才暴露端点）
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// 应用管理的 MCP 访问密钥（外部 MCP 客户端须携带；空则不暴露端点）
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// 携带密钥的请求头名
    /// </summary>
    public string HeaderName { get; set; } = "X-Api-Key";

    /// <summary>
    /// 端点路径
    /// </summary>
    public string Path { get; set; } = "/mcp";

    /// <summary>
    /// 是否无状态 HTTP（无服务端→客户端回调；检索类工具足够，默认 true）
    /// </summary>
    public bool Stateless { get; set; } = true;

    /// <summary>
    /// 是否已就绪暴露（启用 + 配了密钥）
    /// </summary>
    public bool IsExposable => Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}
