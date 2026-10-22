// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Application.AccessPolicies;

/// <summary>
/// 命中记录器：请求线程只往内存里记，由后台服务按分钟批量落库
/// </summary>
public interface IAccessPolicyHitRecorder
{
    /// <summary>
    /// 记一次命中，不阻塞、不抛异常
    /// </summary>
    void Record(AccessPolicyHitEntry entry);

    /// <summary>
    /// 取出已经结束的分钟里合并好的命中
    /// </summary>
    /// <param name="now">当前时刻</param>
    /// <param name="includeCurrentMinute">是否连当前分钟一起取（停机时用）</param>
    /// <returns>每个合并键一项：这一分钟里的第一次命中与次数</returns>
    IReadOnlyList<AccessPolicyHitBatchItem> Drain(DateTimeOffset now, bool includeCurrentMinute = false);
}
