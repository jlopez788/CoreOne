using System.Security.Cryptography;
using System.Text;

namespace CoreOne.Cryptography;

public class CypherService(CryptKey key) : PlainService, ICypher
{
    private enum ProcessType
    {
        Encrypt = 1,
        Decrypt = -1
    }

    private const byte EXPIRES = 1;
    private const byte NO_EXPIRE = 0;
    private static readonly byte[] HEADER = Encoding.ASCII.GetBytes("JLZ");

    public IResult<string, DecryptionStatus> Decrypt(string? data, Encoding? encoding = null)
    {
        if (string.IsNullOrEmpty(data))
            return Invalid<string>("Invalid data", DecryptionStatus.InvalidData);

        try
        {
            var buffer = Convert.FromBase64String(data);
            encoding ??= Encoding.UTF8;
            var result = Decrypt(buffer);
            if (result.IsSuccessStatusCode)
            {
                if (result?.Model?.Length > 0)
                {
                    var content = encoding.GetString(result.Model);
                    return DecryptedOk(content);
                }
                return DecryptedOk(string.Empty);
            }

            return Invalid<string>(result.Message ?? "Invalid data", result.StatusCode, result.ResultType);
        }
        catch (FormatException fex)
        {
            return Invalid<string>(fex.Message, DecryptionStatus.InvalidData, ResultType.Fail);
        }
        catch (Exception ex)
        {
            return Invalid<string>(ex.Message, DecryptionStatus.Error, ResultType.Exception);
        }
    }

    public string Encrypt(string data, DateTime? expiresOnUtc = null, Encoding? encoding = null)
    {
        if (string.IsNullOrWhiteSpace(data))
            data = string.Empty;

        encoding ??= Encoding.UTF8;
        var response = Utility.Try(() => Convert.ToBase64String(Encrypt(encoding.GetBytes(data), expiresOnUtc)));
        return response.ResultType == ResultType.Success && response.Model != null ? response.Model : string.Empty;
    }

    protected override IResult<byte[], DecryptionStatus> OnDecryptData(byte[]? data)
    {
        if ((data != null) && (data.Length > 4))
        {
            using var mem = new MemoryStream(data);
            using var header = Pool.Rent<byte>(HEADER.Length + sizeof(byte) + sizeof(int));
            byte[] tdesiv;
            int n;
            try
            {
                var offset = header.Size;
                mem.Read(header, 0, header.Size);
                if (header[HEADER.Length] == EXPIRES)
                {// check expiration
                    using var expireData = Pool.Rent<byte>(sizeof(long));
                    mem.Read(expireData, 0, expireData.Size);
                    offset += expireData.Size;
                    var ticks = BitConverter.ToInt64(expireData.Array, 0);
                    var expires = DateTime.FromBinary(ticks);
                    if (DateTime.UtcNow > expires)
                        return Invalid<byte[]>("Expired data", DecryptionStatus.Expired);
                }
                n = BitConverter.ToInt32(header, HEADER.Length + 1);
                using var contents = Pool.Rent<byte>(n);
                tdesiv = new byte[data.Length - (n + offset)];
                mem.Read(contents, 0, n); // Contents
                mem.Read(tdesiv, 0, tdesiv.Length); // Triple Des IV

                tdesiv = Process(tdesiv, ProcessType.Decrypt);
                using var ms = new MemoryStream();
                var tdes = Aes.Create();
                tdes.Key = key.Passphrase;
                tdes.IV = tdesiv;
                using (var cs = new CryptoStream(ms, tdes.CreateDecryptor(), CryptoStreamMode.Write))
                    cs.Write(contents, 0, n);
                return DecryptedOk(ms.ToArray());
            }
            catch (Exception ex)
            {
                return Invalid<byte[]>(ex.Message, DecryptionStatus.Error, ResultType.Exception);
            }
        }
        return Invalid<byte[]>("Invalid data", DecryptionStatus.InvalidData);
    }

    protected override byte[] OnEncryptData(byte[]? data, DateTime? expiresOnUtc = null)
    {
        byte[]? buffer = null;
        if ((data != null) && (data.Length > 0))
        {
            using var tdes = CreateCryptography();
            using var mem = new MemoryStream();
            using (var cs = new CryptoStream(mem, tdes.CreateEncryptor(), CryptoStreamMode.Write))
                cs.Write(data, 0, data.Length);
            byte[]
                contents = mem.ToArray(),
                tdesiv = Process(tdes.IV, ProcessType.Encrypt);
            var expires = expiresOnUtc.HasValue;
            using var ms = new MemoryStream();
            ms.Write(HEADER, 0, HEADER.Length);
            ms.WriteByte(expires ? EXPIRES : NO_EXPIRE);
            ms.Write(BitConverter.GetBytes(contents.Length), 0, sizeof(int));
            if (expiresOnUtc.HasValue)
            {
                var dateBuffer = BitConverter.GetBytes(expiresOnUtc.Value.Ticks);
                ms.Write(dateBuffer, 0, dateBuffer.Length);
            }
            ms.Write(contents, 0, contents.Length);
            ms.Write(tdesiv, 0, tdesiv.Length);
            buffer = ms.ToArray();
        }
        return buffer ?? [];
    }

    private Aes CreateCryptography()
    {
        var tdes = Aes.Create();
        tdes.Key = key.Passphrase;
        tdes.GenerateIV();
        return tdes;
    }

    private byte[] Process(byte[] data, ProcessType type)
    {
        byte[] buffer;
        using var ms = new MemoryStream();
        var tdes = Aes.Create();
        tdes.Key = key.Salt;
        tdes.IV = key.IV;
        ICryptoTransform ict = (type == ProcessType.Encrypt) ? tdes.CreateEncryptor() : tdes.CreateDecryptor();
        using (var cs = new CryptoStream(ms, ict, CryptoStreamMode.Write))
        {
            cs.Write(data, 0, data.Length);
            cs.Flush();
            cs.Close();
        }
        ms.Flush();
        buffer = ms.ToArray();
        return buffer;
    }
}