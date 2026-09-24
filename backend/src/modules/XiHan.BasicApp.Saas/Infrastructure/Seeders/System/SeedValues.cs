// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Infrastructure.Seeders.System;

/// <summary>
/// 种子写入的共用小工具
/// </summary>
public static class SeedValues
{
    /// <summary>
    /// 值不同时才赋值，返回是否改动（种子据此决定要不要落库）
    /// </summary>
    public static bool SetIfChanged<T>(T current, T next, Action<T> setter)
    {
        if (EqualityComparer<T>.Default.Equals(current, next))
        {
            return false;
        }

        setter(next);
        return true;
    }
}
