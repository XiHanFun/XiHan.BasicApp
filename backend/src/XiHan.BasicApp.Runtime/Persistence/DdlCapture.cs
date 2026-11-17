// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Persistence;

/// <summary>
/// 收集一段调用里发出的结构语句
/// </summary>
/// <remarks>
/// 结构升级按实体建表补列时用，记进升级记录：经 <see cref="SqlDiagnostics"/> 挂在 AOP 事件上，按调用链（异步本地值）收集，同时跑的别的调用不混进来
/// 只收建表、改表、删表、建索引与列说明的语句；读库结构的查询与读写数据的语句不收
/// </remarks>
public sealed class DdlCapture : IDisposable
{
    private static readonly AsyncLocal<DdlCapture?> _active = new();
    private static readonly string[] _prefixes = ["CREATE ", "ALTER ", "DROP ", "COMMENT ", "EXEC SP_ADDEXTENDEDPROPERTY", "EXECUTE SP_ADDEXTENDEDPROPERTY", "EXEC SP_UPDATEEXTENDEDPROPERTY", "EXECUTE SP_UPDATEEXTENDEDPROPERTY"];

    private readonly DdlCapture? _previous;
    private readonly List<string> _statements = [];
    private readonly Lock _lock = new();

    private DdlCapture(DdlCapture? previous) => _previous = previous;

    /// <summary>
    /// 当前调用链上正在收集的
    /// </summary>
    internal static DdlCapture? Active => _active.Value;

    /// <summary>
    /// 收到的语句（按发出的先后）
    /// </summary>
    public IReadOnlyList<string> Statements
    {
        get
        {
            lock (_lock)
            {
                return [.. _statements];
            }
        }
    }

    /// <summary>
    /// 开始收集
    /// </summary>
    /// <returns>释放即停止收集</returns>
    public static DdlCapture Begin()
    {
        var capture = new DdlCapture(_active.Value);
        _active.Value = capture;
        return capture;
    }

    /// <inheritdoc />
    public void Dispose() => _active.Value = _previous;

    /// <summary>
    /// 记下一条语句（不是结构语句即忽略）
    /// </summary>
    internal void Record(string? sql)
    {
        var text = sql?.Trim();
        if (string.IsNullOrEmpty(text) || !_prefixes.Any(prefix => text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        lock (_lock)
        {
            _statements.Add(text);
        }
    }
}
