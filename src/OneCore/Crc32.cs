using System.Security.Cryptography;
using System.Text;

namespace CoreOne;

public sealed class Crc32 : HashAlgorithm
{
    public const uint DefaultPolynomial = 0xedb88320u;
    public const uint DefaultSeed = 0xffffffffu;
    private static uint[]? StaticTable;
    private readonly uint Seed;
    private readonly uint[] Table;
    private uint NumHash;

    public override int HashSize => 32;

    public Crc32() : this(DefaultPolynomial, DefaultSeed)
    {
    }

    public Crc32(uint polynomial, uint seed)
    {
        if (!BitConverter.IsLittleEndian)
            throw new PlatformNotSupportedException("Not supported on Big Endian processors");

        Table = InitializeTable(polynomial);
        Seed = NumHash = seed;
    }

    public static int Compute(string? data) => string.IsNullOrWhiteSpace(data) ? 0 : (int)Compute(Encoding.UTF8.GetBytes(data));

    public static uint Compute(byte[] buffer) => Compute(DefaultSeed, buffer);

    public static uint Compute(uint seed, byte[] buffer) => Compute(DefaultPolynomial, seed, buffer);

    public static uint Compute(uint polynomial, uint seed, byte[] buffer) => ~CalculateHash(InitializeTable(polynomial), seed, buffer, 0, buffer.Length);

    public override void Initialize() => NumHash = Seed;

    protected override void HashCore(byte[] array, int ibStart, int cbSize) => NumHash = CalculateHash(Table, NumHash, array, ibStart, cbSize);

    protected override byte[] HashFinal()
    {
        var hashBuffer = UInt32ToBigEndianBytes(~NumHash);
        HashValue = hashBuffer;
        return hashBuffer;
    }

    private static uint CalculateHash(uint[] table, uint seed, byte[] buffer, int start, int size)
    {
        var hash = seed;
        for (var i = start; i < start + size; i++)
            hash = (hash >> 8) ^ table[buffer[i] ^ (hash & 0xff)];
        return hash;
    }

    private static uint[] InitializeTable(uint polynomial)
    {
        if (polynomial == DefaultPolynomial && StaticTable is not null)
            return StaticTable;

        var createTable = new uint[256];
        for (var i = 0; i < 256; i++)
        {
            var entry = (uint)i;
            for (var j = 0; j < 8; j++)
                if ((entry & 1) == 1)
                    entry = (entry >> 1) ^ polynomial;
                else
                    entry >>= 1;
            createTable[i] = entry;
        }

        if (polynomial == DefaultPolynomial)
            StaticTable = createTable;

        return createTable;
    }

    private static byte[] UInt32ToBigEndianBytes(uint uint32)
    {
        var result = BitConverter.GetBytes(uint32);

        if (BitConverter.IsLittleEndian)
            Array.Reverse(result);

        return result;
    }
}