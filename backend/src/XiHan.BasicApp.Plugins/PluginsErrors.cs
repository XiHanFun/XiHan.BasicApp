// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Plugins;

/// <summary>
/// 插件模块的错误码
/// </summary>
/// <remarks>
/// 与框架插件宿主的错误码同一个前缀 <c>PLUGINS_</c>：框架占 001 段，BasicApp 自己的各类从 101 起编，前端按码就分得清是谁报的
/// 框架的失败（清单读不出的 <c>KERNEL_VALIDATION_*</c>、加载时的 <c>PLUGINS_*_001</c> 段）原样返回，不另起码
/// </remarks>
public static class PluginsErrors
{
    /// <summary>
    /// 插件不存在
    /// </summary>
    public const string PluginNotFound = "PLUGINS_NOTFOUND_101";

    /// <summary>
    /// 插件没有这个版本
    /// </summary>
    public const string VersionNotFound = "PLUGINS_NOTFOUND_102";

    /// <summary>
    /// 插件包读不出、过大或结构不对
    /// </summary>
    public const string PackageInvalid = "PLUGINS_VALIDATION_101";

    /// <summary>
    /// 旁挂清单读不出或不合规则
    /// </summary>
    public const string DescriptorInvalid = "PLUGINS_VALIDATION_102";

    /// <summary>
    /// 框架清单的种类、隔离级别或贡献不被接受
    /// </summary>
    public const string ManifestRejected = "PLUGINS_VALIDATION_103";

    /// <summary>
    /// 短名不合文法或是保留名
    /// </summary>
    public const string NameInvalid = "PLUGINS_VALIDATION_104";

    /// <summary>
    /// 贡献的操作、权限码、页面或回调不合命名规则
    /// </summary>
    public const string ContributionInvalid = "PLUGINS_VALIDATION_105";

    /// <summary>
    /// 授予里有不认得的项
    /// </summary>
    public const string GrantInvalid = "PLUGINS_VALIDATION_106";

    /// <summary>
    /// 列表不支持该筛选或排序
    /// </summary>
    public const string QueryUnsupported = "PLUGINS_VALIDATION_107";

    /// <summary>
    /// 加载后插件的消息目录缺少分组或资源标签的显示名
    /// </summary>
    public const string CatalogIncomplete = "PLUGINS_VALIDATION_108";

    /// <summary>
    /// 签名文件读不出或签名与包内容不符
    /// </summary>
    public const string SignatureInvalid = "PLUGINS_VALIDATION_109";

    /// <summary>
    /// 升级用的包是另一个插件
    /// </summary>
    public const string UpgradeMismatch = "PLUGINS_VALIDATION_110";

    /// <summary>
    /// 清单 ID 或短名已经安装过
    /// </summary>
    public const string AlreadyInstalled = "PLUGINS_CONFLICT_101";

    /// <summary>
    /// 贡献的操作或权限码与已登记的重复
    /// </summary>
    public const string ContributionConflict = "PLUGINS_CONFLICT_102";

    /// <summary>
    /// 升级用的版本不比当前版本新
    /// </summary>
    public const string VersionNotNewer = "PLUGINS_CONFLICT_103";

    /// <summary>
    /// 升级用的版本以前上传过
    /// </summary>
    public const string VersionUploaded = "PLUGINS_CONFLICT_104";

    /// <summary>
    /// 要换回的版本已经是当前版本
    /// </summary>
    public const string VersionCurrent = "PLUGINS_CONFLICT_105";

    /// <summary>
    /// 插件申请的授予没有全部批准
    /// </summary>
    public const string GrantsMissing = "PLUGINS_DENIED_101";

    /// <summary>
    /// 插件包没有签名，且不放行没有签名的包
    /// </summary>
    public const string PackageUnsigned = "PLUGINS_DENIED_102";

    /// <summary>
    /// 签名的公钥不在受信任的发布者里，且不放行
    /// </summary>
    public const string PublisherUntrusted = "PLUGINS_DENIED_103";

    /// <summary>
    /// 宿主没有装这个隔离级别的插件宿主
    /// </summary>
    public const string IsolationUnavailable = "PLUGINS_UNAVAILABLE_101";

    /// <summary>
    /// 版本的包文件不在了或与安装时的散列不一致
    /// </summary>
    public const string PackageUnavailable = "PLUGINS_UNAVAILABLE_102";
}
