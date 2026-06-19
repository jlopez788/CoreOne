namespace CoreOne.Models;

public class WebCode
{
    public static implicit operator bool(WebCode? webCode) => webCode?.IsSuccessStatusCode == true;

    public int Code { get; init; }
    public bool IsSuccessStatusCode { get; }

    public WebCode(int code)
    {
        Code = code;
        IsSuccessStatusCode = WebCodes.IsSuccessStatusCode(code);
    }

    public WebCode(int? code)
    {
        Code = code.GetValueOrDefault();
        IsSuccessStatusCode = WebCodes.IsSuccessStatusCode(Code);
    }
}