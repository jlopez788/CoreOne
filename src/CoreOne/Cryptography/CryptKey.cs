using System.Text;

namespace CoreOne.Cryptography;

public readonly struct CryptKey
{
    public byte[] Passphrase { get; }
    public byte[] Salt { get; init; } = Encoding.ASCII.GetBytes("E6qbsDjQZdUhjujy");
    public byte[] IV { get; init; } = Encoding.ASCII.GetBytes("pKMv5vzBjsN7scRc");

    public CryptKey(string passphrase)
    {
        var result = Utility.HashMD5(passphrase)
             .Select(Encoding.UTF8.GetBytes);
        Passphrase = result.Model ?? [];
    }
}