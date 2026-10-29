// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Files;

/// <summary>
/// 头像文件（Files 实现）：头像固定存平台侧、公开级别（任何已登录主体不分侧都能换签名地址）。Identity 换头像或去掉头像时经它回收旧文件；没装 Files 即没有头像。
/// </summary>
public interface IAvatarFiles
{
    /// <summary>
    /// 回收不再用的头像文件（软删除，存储里的对象不再被引用即随之删除）；已经不在的照常返回。
    /// </summary>
    /// <param name="fileId">头像文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task ReleaseAsync(Guid fileId, CancellationToken cancellationToken);
}
