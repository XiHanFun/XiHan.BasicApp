// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Files;

/// <summary>
/// 站点图片（Files 实现）
/// </summary>
/// <remarks>
/// 站点设置与租户品牌的 Logo、浏览器图标与登录页背景固定存平台侧、公开级别，登录前匿名可读（宿主的 <c>/api/site/assets/{id}</c>），不计入租户的存储配额
/// Settings 换掉或去掉图片时经它回收旧文件；没装 Files 即没有站点图片
/// </remarks>
public interface ISiteAssetFiles
{
    /// <summary>
    /// 回收不再用的站点图片
    /// </summary>
    /// <remarks>
    /// 软删除，存储里的对象不再被引用即随之删除；已经不在的照常返回
    /// </remarks>
    /// <param name="fileId">图片文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task ReleaseAsync(Guid fileId, CancellationToken cancellationToken);
}
