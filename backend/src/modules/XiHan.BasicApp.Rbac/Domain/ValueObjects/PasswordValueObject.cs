using XiHan.Framework.Authentication.Password;

namespace XiHan.BasicApp.Rbac.Domain.ValueObjects;

/// <summary>
/// 密码值对象（仅承载哈希值与校验语义）
/// </summary>
public readonly record struct PasswordValueObject
{
    /// <summary>
    /// 哈希后的密码字符串
    /// </summary>
    public string Hash { get; }

    private PasswordValueObject(string hash)
    {
        Hash = hash;
    }

    /// <summary>
    /// 由明文密码创建值对象
    /// </summary>
    public static PasswordValueObject Create(string plainPassword, IPasswordHasher passwordHasher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        return new PasswordValueObject(passwordHasher.HashPassword(plainPassword));
    }

    /// <summary>
    /// 由已有哈希创建值对象
    /// </summary>
    public static PasswordValueObject FromHash(string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        return new PasswordValueObject(hash);
    }

    /// <summary>
    /// 校验明文密码
    /// </summary>
    public bool Verify(string plainPassword, IPasswordHasher passwordHasher)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(plainPassword);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        return passwordHasher.VerifyPassword(Hash, plainPassword);
    }
}
