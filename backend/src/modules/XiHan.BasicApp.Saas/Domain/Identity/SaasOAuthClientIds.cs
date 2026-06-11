#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasOAuthClientIds
// Guid:3f8a2b91-6c47-4d05-9e1a-7b2c8d4f6a03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/11 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.Identity;

/// <summary>
/// SaaS 内建 OAuth 客户端标识常量（单一事实源）
/// </summary>
/// <remarks>
/// 与 SysOAuthApp.ClientId 对应：登录会话签发、系统参数默认值、种子数据均以常量引用，禁止散落魔法字符串。
/// 新增内建客户端必须在此登记，并在 <c>SaasOAuthAppSeeder</c> 同步补种。
/// </remarks>
public static class SaasOAuthClientIds
{
    /// <summary>
    /// 平台自营 Web 前端（第一方公开客户端，密码登录签发 Token 时的默认 ClientId）
    /// </summary>
    public const string Web = "basicapp-web";

    /// <summary>
    /// 默认权限范围
    /// </summary>
    public const string DefaultScope = "basicapp";
}
