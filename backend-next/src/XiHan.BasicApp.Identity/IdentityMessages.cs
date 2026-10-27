// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Identity;

/// <summary>
/// 身份包的消息键；各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。文案不进代码：错误只带键与参数，由宿主在 Egress 按调用方区域翻译。
/// </summary>
public static class IdentityMessages
{
    /// <summary>消息目录名。</summary>
    public const string CatalogName = "basicapp.identity";

    /// <summary>操作 <c>users.create</c> 的说明（接口文档摘要与 MCP tool 描述共用键 <c>&lt;操作 ID&gt;.description</c>）。</summary>
    public const string CreateUserDescription = "users.create.description";

    /// <summary>操作 <c>users.get</c> 的说明。</summary>
    public const string GetUserDescription = "users.get.description";

    /// <summary>操作 <c>users.list</c> 的说明。</summary>
    public const string ListUsersDescription = "users.list.description";

    /// <summary>用户不存在；参数 <c>id</c>。</summary>
    public const string UserNotFound = "identity.user.not_found";

    /// <summary>用户名已被占用；参数 <c>userName</c>、<c>tenant</c>。</summary>
    public const string UserNameTaken = "identity.user.name_taken";

    /// <summary>创建用户需要当前租户。</summary>
    public const string TenantRequired = "identity.user.tenant_required";

    /// <summary>显示名只有空白；<c>CreateUserRequest</c> 自校验的文案。</summary>
    public const string DisplayNameBlank = "identity.user.display_name_blank";

    /// <summary>邮箱格式不对；<c>CreateUserRequest</c> 自校验的文案。</summary>
    public const string EmailInvalid = "identity.user.email_invalid";

    /// <summary>列表筛选不受支持；参数 <c>field</c>、<c>operator</c>。</summary>
    public const string FilterUnsupported = "identity.user.filter_unsupported";

    /// <summary>列表排序字段不受支持；参数 <c>field</c>。</summary>
    public const string SortUnsupported = "identity.user.sort_unsupported";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(IdentityMessages)));

    /// <summary>
    /// 本包的消息目录（嵌入的 10 种区域文件）。
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
