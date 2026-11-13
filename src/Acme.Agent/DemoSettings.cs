using Microsoft.Extensions.AI;

namespace Acme.Agent;

public sealed record DemoSettings(Guid TenantId, Guid UserId, string CatalogPath, string MemoryRoot)
{
    public static DemoSettings Load()
    {
        var catalog = Environment.GetEnvironmentVariable("OKF_CATALOG_PATH") ?? Path.Combine(
            Path.GetTempPath(), "amp-lyon-2026-demo1", "artifacts", "acme", "catalog.json");
        if (!File.Exists(catalog))
            throw new InvalidOperationException("Catalogue Acme introuvable. Suivre la préparation du README.");
        var memory = Environment.GetEnvironmentVariable("OKF_MEMORY_ROOT") ?? Path.Combine(
            Path.GetTempPath(), "amp-lyon-2026-demo1", "memory");
        return new(ReadId("ACME_TENANT_ID"), ReadId("ACME_USER_ID"), catalog, memory);
    }

    public static IChatClient OpenClient() => AcmeAgent.ConnectOpenAI(
        new Uri(Required("OKF_CHAT_BASE_URL")), Required("OKF_CHAT_MODEL"), Required("OKF_CHAT_API_KEY"));

    public static string Required(string name) =>
        Environment.GetEnvironmentVariable(name)?.Trim() is { Length: > 0 } value
            ? value : throw new InvalidOperationException($"Configurer {name} dans ce terminal.");

    public static Guid ReadId(string name) =>
        Guid.TryParse(Required(name), out var id) && id != Guid.Empty
            ? id : throw new InvalidOperationException($"{name} doit contenir un GUID Entra valide.");
}
