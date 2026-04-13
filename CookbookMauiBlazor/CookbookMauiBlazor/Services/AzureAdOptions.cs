namespace CookbookMauiBlazor.Services
{
    public sealed class AzureAdOptions
    {
        public string? Instance { get; set; }
        public string? TenantId { get; set; }
        public string? ClientId { get; set; }
        public string[] Scopes { get; set; } = Array.Empty<string>();
    }
}
