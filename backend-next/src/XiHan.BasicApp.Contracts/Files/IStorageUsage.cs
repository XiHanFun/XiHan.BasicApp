// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Files;

/// <summary>
/// 存储用量（Files 实现）：各租户未删除文件的大小之和（字节），共用同一对象的文件各自计入（4.x 同按记录计）。没装 Files 即没有用量。
/// </summary>
public interface IStorageUsage
{
    /// <summary>
    /// 一批租户各自的已用字节数；没有文件的租户不在结果里。
    /// </summary>
    /// <param name="tenantIds">租户 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>按租户 ID 索引的已用字节数。</returns>
    Task<IReadOnlyDictionary<string, long>> UsedBytesAsync(IReadOnlyCollection<string> tenantIds, CancellationToken cancellationToken);
}
