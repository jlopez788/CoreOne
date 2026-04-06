namespace CoreOne.Cryptography;

public class PlainService : ICypher
{
    protected class DecryptionContent<T> : IResult<T, DecryptionStatus>
    {
#if NET9_0_OR_GREATER
        [MemberNotNullWhen(false, nameof(Message))]
        [MemberNotNullWhen(true, nameof(Model))]
#endif
        public bool IsSuccessStatusCode => StatusCode == DecryptionStatus.Ok;
        public string? Message { get; init; }
        public T? Model { get; init; }
        public ResultType ResultType { get; init; }
        public DecryptionStatus StatusCode { get; init; }
        public bool Success => ResultType == ResultType.Success && StatusCode == DecryptionStatus.Ok;
    }

    public static ICypher Default { get; } = new PlainService();

    public IResult<byte[], DecryptionStatus> Decrypt(byte[]? data) => OnDecryptData(data);

    public byte[] Encrypt(byte[]? data, DateTime? expiresOnUtc = null) => OnEncryptData(data, expiresOnUtc);

    protected static DecryptionContent<T> DecryptedOk<T>(T content) => new() {
        Model = content,
        StatusCode = DecryptionStatus.Ok,
        ResultType = ResultType.Success
    };

    protected static DecryptionContent<T> Invalid<T>(string msg, DecryptionStatus status, ResultType resultType = ResultType.Fail) => new() {
        Message = msg,
        ResultType = resultType,
        StatusCode = status
    };

    protected virtual IResult<byte[], DecryptionStatus> OnDecryptData(byte[]? data) => data?.Length > 0 ?
        DecryptedOk(data) :
        Invalid<byte[]>("Invalid data", DecryptionStatus.InvalidData);

    protected virtual byte[] OnEncryptData(byte[]? data, DateTime? expiresOnUtc = null) => data ?? [];
}