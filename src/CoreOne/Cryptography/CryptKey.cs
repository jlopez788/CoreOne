using System.Text;

namespace CoreOne.Cryptography;

public readonly struct CryptKey(string passphrase)
{
    public byte[] Passphrase { get; } = Encoding.ASCII.GetBytes(passphrase);
    public byte[] Salt { get; init; } = Encoding.ASCII.GetBytes("E6qbsDjQZdUhjujy");
    public byte[] IV { get; init; } = Encoding.ASCII.GetBytes("pKMv5vzBjsN7scRc");
}
