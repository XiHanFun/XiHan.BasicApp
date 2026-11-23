// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Localization.Abstractions.Messages;

namespace XiHan.BasicApp.Plugins;

/// <summary>
/// 插件模块的消息键
/// </summary>
/// <remarks>
/// 各区域文案在 <c>Localization/&lt;区域&gt;.json</c>。操作说明用约定键 <c>&lt;操作 ID&gt;.description</c>（资源段的 <c>-</c> 换成 <c>_</c>）
/// 与框架插件宿主共用首段 <c>plugins.</c>，二级段避开框架已用的（<c>assembly</c>、<c>callback</c>、<c>entry</c>、<c>event</c>、<c>handshake</c>、<c>host</c>、<c>manifest</c>、<c>module</c>、<c>permission</c>、<c>plugin</c>、<c>process</c>、<c>sandbox</c>）
/// </remarks>
public static class PluginsMessages
{
    /// <summary>
    /// 消息目录名
    /// </summary>
    public const string CatalogName = "basicapp.plugins";

    /// <summary>
    /// 操作 <c>plugins.list</c> 的说明
    /// </summary>
    public const string ListPluginsDescription = "plugins.list.description";

    /// <summary>
    /// 操作 <c>plugins.get</c> 的说明
    /// </summary>
    public const string GetPluginDescription = "plugins.get.description";

    /// <summary>
    /// 操作 <c>plugins.inspect</c> 的说明
    /// </summary>
    public const string InspectPluginDescription = "plugins.inspect.description";

    /// <summary>
    /// 操作 <c>plugins.install</c> 的说明
    /// </summary>
    public const string InstallPluginDescription = "plugins.install.description";

    /// <summary>
    /// 操作 <c>plugins.grant</c> 的说明
    /// </summary>
    public const string GrantPluginDescription = "plugins.grant.description";

    /// <summary>
    /// 操作 <c>plugins.delete</c> 的说明
    /// </summary>
    public const string DeletePluginsDescription = "plugins.delete.description";

    /// <summary>
    /// 操作 <c>plugins.enable</c> 的说明
    /// </summary>
    public const string EnablePluginsDescription = "plugins.enable.description";

    /// <summary>
    /// 操作 <c>plugins.disable</c> 的说明
    /// </summary>
    public const string DisablePluginsDescription = "plugins.disable.description";

    /// <summary>
    /// 操作 <c>plugins.status</c> 的说明
    /// </summary>
    public const string GetPluginStatusDescription = "plugins.status.description";

    /// <summary>
    /// 操作 <c>plugins.delete-preview</c> 的说明
    /// </summary>
    public const string PreviewPluginDeleteDescription = "plugins.delete_preview.description";

    /// <summary>
    /// 操作 <c>plugin-views.list</c> 的说明
    /// </summary>
    public const string ListPluginViewsDescription = "plugin_views.list.description";

    /// <summary>
    /// 插件不存在
    /// </summary>
    /// <remarks>
    /// 参数：<c>id</c>
    /// </remarks>
    public const string PluginNotFound = "plugins.installation.not_found";

    /// <summary>
    /// 上传的文件不是能读的插件包
    /// </summary>
    public const string PackageUnreadable = "plugins.package.unreadable";

    /// <summary>
    /// 插件包超过上限
    /// </summary>
    /// <remarks>
    /// 参数：<c>max</c>（上限，MB）
    /// </remarks>
    public const string PackageTooLarge = "plugins.package.too_large";

    /// <summary>
    /// 插件包里有不安全的路径
    /// </summary>
    /// <remarks>
    /// 参数：<c>entry</c>
    /// </remarks>
    public const string PackageEntryUnsafe = "plugins.package.entry_unsafe";

    /// <summary>
    /// 插件包缺少必需的文件
    /// </summary>
    /// <remarks>
    /// 参数：<c>entry</c>
    /// </remarks>
    public const string PackageEntryMissing = "plugins.package.entry_missing";

    /// <summary>
    /// 插件包没有当前平台的运行时
    /// </summary>
    /// <remarks>
    /// 参数：<c>runtime</c>
    /// </remarks>
    public const string PackageRuntimeMissing = "plugins.package.runtime_missing";

    /// <summary>
    /// 运行时目录里的文件与包根上的同名
    /// </summary>
    /// <remarks>
    /// 参数：<c>path</c>
    /// </remarks>
    public const string PackageRuntimeConflict = "plugins.package.runtime_conflict";

    /// <summary>
    /// 插件包缺少某种区域的前端文案
    /// </summary>
    /// <remarks>
    /// 参数：<c>locale</c>
    /// </remarks>
    public const string PackageLocaleMissing = "plugins.package.locale_missing";

    /// <summary>
    /// 插件包某种区域的前端文案不合规则
    /// </summary>
    /// <remarks>
    /// 参数：<c>locale</c>、<c>key</c>（不合规则的第一个键，整份读不出为 <c>$</c>）
    /// </remarks>
    public const string PackageLocaleInvalid = "plugins.package.locale_invalid";

    /// <summary>
    /// 框架清单的种类不是插件
    /// </summary>
    public const string PackageKindInvalid = "plugins.package.kind_invalid";

    /// <summary>
    /// 不允许的隔离级别
    /// </summary>
    /// <remarks>
    /// 参数：<c>isolation</c>
    /// </remarks>
    public const string PackageIsolationNotAllowed = "plugins.package.isolation_not_allowed";

    /// <summary>
    /// 插件贡献了管道中间件
    /// </summary>
    public const string PackagePipelineUnsupported = "plugins.package.pipeline_unsupported";

    /// <summary>
    /// 旁挂清单读不出或不合格式
    /// </summary>
    /// <remarks>
    /// 参数：<c>path</c>（出问题的 JSON 路径）
    /// </remarks>
    public const string DescriptorInvalid = "plugins.descriptor.invalid";

    /// <summary>
    /// 旁挂清单的接口版本不受支持
    /// </summary>
    /// <remarks>
    /// 参数：<c>version</c>
    /// </remarks>
    public const string DescriptorApiUnsupported = "plugins.descriptor.api_unsupported";

    /// <summary>
    /// 短名不合文法
    /// </summary>
    /// <remarks>
    /// 参数：<c>name</c>
    /// </remarks>
    public const string NameInvalid = "plugins.name.invalid";

    /// <summary>
    /// 短名是保留名
    /// </summary>
    /// <remarks>
    /// 参数：<c>name</c>
    /// </remarks>
    public const string NameReserved = "plugins.name.reserved";

    /// <summary>
    /// 操作的资源段不在短名之下
    /// </summary>
    /// <remarks>
    /// 参数：<c>id</c>、<c>name</c>
    /// </remarks>
    public const string OperationOutsideName = "plugins.contribution.operation_outside";

    /// <summary>
    /// 旁挂清单与框架清单的权限码不一致
    /// </summary>
    public const string CodesMismatch = "plugins.contribution.codes_mismatch";

    /// <summary>
    /// 权限码不合规则
    /// </summary>
    /// <remarks>
    /// 参数：<c>code</c>
    /// </remarks>
    public const string CodeInvalid = "plugins.contribution.code_invalid";

    /// <summary>
    /// 页面不合规则
    /// </summary>
    /// <remarks>
    /// 参数：<c>code</c>
    /// </remarks>
    public const string PageInvalid = "plugins.contribution.page_invalid";

    /// <summary>
    /// 页面描述不合规则
    /// </summary>
    /// <remarks>
    /// 参数：<c>code</c>、<c>path</c>（不合规则的位置，如 <c>columns[0].key</c>）
    /// </remarks>
    public const string PageViewInvalid = "plugins.contribution.page_view_invalid";

    /// <summary>
    /// 页面用到的文案键 en-US 的前端文案里没有
    /// </summary>
    /// <remarks>
    /// 参数：<c>code</c>、<c>key</c>
    /// </remarks>
    public const string PageTextMissing = "plugins.contribution.page_text_missing";

    /// <summary>
    /// 回调的权限码宿主没有登记
    /// </summary>
    /// <remarks>
    /// 参数：<c>code</c>
    /// </remarks>
    public const string CallbackUnknown = "plugins.contribution.callback_unknown";

    /// <summary>
    /// 要回调却没有申请管道能力
    /// </summary>
    public const string CallbackPipelineRequired = "plugins.contribution.callback_pipeline_required";

    /// <summary>
    /// 扩展点还不支持
    /// </summary>
    public const string ExtendsUnsupported = "plugins.contribution.extends_unsupported";

    /// <summary>
    /// 清单 ID 或短名已经安装过
    /// </summary>
    /// <remarks>
    /// 参数：<c>id</c>（清单 ID 或短名）
    /// </remarks>
    public const string AlreadyInstalled = "plugins.installation.taken";

    /// <summary>
    /// 贡献与已登记的重复
    /// </summary>
    /// <remarks>
    /// 参数：<c>item</c>（重复的操作 ID 或权限码）
    /// </remarks>
    public const string ContributionConflict = "plugins.installation.conflict";

    /// <summary>
    /// 申请的授予没有全部批准
    /// </summary>
    /// <remarks>
    /// 参数：<c>grants</c>（缺的项，逗号分隔）
    /// </remarks>
    public const string GrantsMissing = "plugins.grants.missing";

    /// <summary>
    /// 授予里有不认得的项
    /// </summary>
    /// <remarks>
    /// 参数：<c>grant</c>
    /// </remarks>
    public const string GrantInvalid = "plugins.grants.invalid";

    /// <summary>
    /// 不支持的筛选
    /// </summary>
    /// <remarks>
    /// 参数：<c>field</c>、<c>operator</c>
    /// </remarks>
    public const string FilterUnsupported = "plugins.query.filter_unsupported";

    /// <summary>
    /// 不支持的排序
    /// </summary>
    /// <remarks>
    /// 参数：<c>field</c>
    /// </remarks>
    public const string SortUnsupported = "plugins.query.sort_unsupported";

    /// <summary>
    /// 没有这个隔离级别的插件宿主
    /// </summary>
    /// <remarks>
    /// 参数：<c>isolation</c>
    /// </remarks>
    public const string IsolationUnavailable = "plugins.runtime.isolation_unavailable";

    /// <summary>
    /// 版本的包文件不可用
    /// </summary>
    /// <remarks>
    /// 参数：<c>name</c>、<c>version</c>
    /// </remarks>
    public const string PackageUnavailable = "plugins.runtime.package_unavailable";

    /// <summary>
    /// 加载后插件的消息目录缺少接口文档要用的键
    /// </summary>
    /// <remarks>
    /// 参数：<c>name</c>、<c>key</c>
    /// </remarks>
    public const string CatalogIncomplete = "plugins.runtime.catalog_incomplete";

    /// <summary>
    /// 签名文件读不出
    /// </summary>
    public const string SignatureUnreadable = "plugins.signature.unreadable";

    /// <summary>
    /// 签名与包内容不符
    /// </summary>
    /// <remarks>
    /// 参数：<c>key</c>（签名公钥的指纹）
    /// </remarks>
    public const string SignatureMismatch = "plugins.signature.mismatch";

    /// <summary>
    /// 插件包没有签名
    /// </summary>
    public const string SignatureMissing = "plugins.signature.missing";

    /// <summary>
    /// 签名的公钥不受信任
    /// </summary>
    /// <remarks>
    /// 参数：<c>key</c>（签名公钥的指纹）、<c>publisher</c>
    /// </remarks>
    public const string SignatureUntrusted = "plugins.signature.untrusted";

    private static readonly Lazy<MessageCatalog> _catalog = new(static () => MessageCatalog.FromEmbeddedResources(CatalogName, typeof(PluginsMessages)));

    /// <summary>
    /// 本模块的消息目录（嵌入的 10 种区域文件）
    /// </summary>
    public static MessageCatalog Catalog => _catalog.Value;
}
