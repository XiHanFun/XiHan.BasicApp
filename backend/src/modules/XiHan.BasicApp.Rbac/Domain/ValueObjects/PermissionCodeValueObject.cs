namespace XiHan.BasicApp.Rbac.Domain.ValueObjects;

/// <summary>
/// 权限编码值对象（格式：resource:operation）
/// </summary>
public readonly record struct PermissionCodeValueObject
{
    /// <summary>
    /// 资源编码
    /// </summary>
    public string ResourceCode { get; }

    /// <summary>
    /// 操作编码
    /// </summary>
    public string OperationCode { get; }

    private PermissionCodeValueObject(string resourceCode, string operationCode)
    {
        ResourceCode = resourceCode;
        OperationCode = operationCode;
    }

    /// <summary>
    /// 从资源与操作构建权限编码
    /// </summary>
    public static PermissionCodeValueObject Create(string resourceCode, string operationCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationCode);
        return new PermissionCodeValueObject(resourceCode.Trim(), operationCode.Trim());
    }

    /// <summary>
    /// 解析权限编码
    /// </summary>
    public static PermissionCodeValueObject Parse(string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        var parts = permissionCode.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new ArgumentException("权限编码格式错误，应为 resource:operation", nameof(permissionCode));
        }

        return Create(parts[0], parts[1]);
    }

    /// <summary>
    /// 获取编码字符串
    /// </summary>
    public override string ToString()
    {
        return $"{ResourceCode}:{OperationCode}";
    }
}
