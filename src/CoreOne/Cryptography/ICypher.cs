namespace CoreOne.Cryptography;

public interface ICypher
{
    /// <summary>
    /// Decrypt data
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    IResult<byte[]> Decrypt(byte[]? data);

    /// <summary>
    /// Encrypt data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="expiresOnUtc"></param>
    /// <returns></returns>
    byte[] Encrypt(byte[]? data, DateTime? expiresOnUtc = null);
}