namespace CoreOne.Identity.Models;

public class ClientApp
{
    public string AuthenticationUrl { get; set; }
    public string ClientId { get; set; } = null!;
    public string? HostUrl { get; set; }
    public string? HubUrl { get; set; }
    public string Secret { get; set; }
    public string? SiteName { get; set; }

    public ClientApp()
    {
        Secret = string.Empty;
        AuthenticationUrl = string.Empty;
    }

    public ClientApp(string clientId, string siteName)
    {
        ClientId = clientId;
        Secret = string.Empty;
        SiteName = siteName;
        AuthenticationUrl = string.Empty;
    }
}