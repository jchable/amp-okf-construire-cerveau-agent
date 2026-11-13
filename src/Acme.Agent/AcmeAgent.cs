using System.ClientModel;
using System.ComponentModel;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OKF4net.Agents;
using OKF4net.Catalog;
using OpenAI;

namespace Acme.Agent;

public sealed class AcmeAgent : IDisposable
{
    private readonly FileKnowledgeCatalog catalog;
    public string MemoryRoot { get; }

    public AcmeAgent(string catalogPath, string memoryRoot)
    {
        catalogPath = Path.GetFullPath(catalogPath);
        MemoryRoot = Path.GetFullPath(memoryRoot);
        catalog = new FileKnowledgeCatalog(new KnowledgeCatalogOptions
        {
            CatalogFilePath = catalogPath,
            CatalogRoot = Path.GetDirectoryName(catalogPath)!,
            WatchForChanges = false
        });
    }

    public static IChatClient ConnectOpenAI(Uri endpoint, string model, string apiKey)
    {
        if (endpoint.Scheme != Uri.UriSchemeHttps && !(endpoint.IsLoopback && endpoint.Scheme == Uri.UriSchemeHttp))
            throw new ArgumentException("Utiliser une URL HTTPS pour le service OpenAI.");

        return new OpenAIClient(new ApiKeyCredential(apiKey), new OpenAIClientOptions { Endpoint = endpoint })
            .GetChatClient(model).AsIChatClient();
    }

    // Chaque hôte fournit l’identité de son utilisateur. Teams la tirera de l’activité authentifiée.
    public AIAgent ForUser(IChatClient client, Guid tenantId, Guid userId)
    {
        if (tenantId == Guid.Empty || userId == Guid.Empty)
            throw new ArgumentException("Le tenant ID et l’Object ID utilisateur doivent être renseignés.");

        var scope = new KnowledgeAccessScope(tenantId: tenantId.ToString("D"), userId: userId.ToString("D"));
        var knowledgeTools = new OkfBundleTools(catalog.Current.Handles["acme"]);
        var preference = new BriefingPreference(MemoryRoot, scope);
        var store = new FileMemoryStore(new Dictionary<MemoryTier, string> { [MemoryTier.User] = MemoryRoot });
        var context = new OkfContextProvider(new GroupedKnowledgeResolver(catalog), store,
            new OkfContextProviderOptions { ScopeAccessor = _ => scope, MemoryCapture = MemoryCaptureMode.Disabled });

        return client.AsAIAgent(new ChatClientAgentOptions
        {
            ChatOptions = new ChatOptions
            {
                Instructions = """
                    Réponds en français. Pour une question métier, cherche dans le bundle Acme avec
                    okf_search (termes anglais si utile), lis la source avec okf_read_concept et cite son
                    identifiant exact. Si la source est insuffisante, dis-le. Pour préparer un briefing,
                    consulte lire_preference et applique le format enregistré s’il existe.
                    Si l’utilisateur te donne une préférence à mémoriser, appelle memoriser_preference
                    avec sa formulation exacte. Confirme uniquement après une écriture réussie.
                    Une préférence porte sur la présentation et ne change pas les règles métier.
                    """,
                Tools = [
                    .. knowledgeTools.GetTools(OkfToolMode.ReadOnly),
                    AIFunctionFactory.Create(preference.ReadAsync, "lire_preference"),
                    AIFunctionFactory.Create(preference.SaveAsync, "memoriser_preference")
                ]
            },
            AIContextProviders = [context]
        });
    }

    public void Dispose() => catalog.Dispose();
}

internal sealed class BriefingPreference
{
    private const string Concept = "briefing";
    private readonly OkfBundleTools memory;

    public BriefingPreference(string root, KnowledgeAccessScope scope)
    {
        var directory = Directory.CreateDirectory(Path.Combine(root, MemoryPath.For(MemoryTier.User, scope)));
        memory = new OkfBundleTools(directory.FullName);
    }

    [Description("Lit la préférence de présentation du briefing pour cet utilisateur.")]
    public Task<string> ReadAsync(CancellationToken ct) => memory.ReadConceptAsync(Concept, ct);

    [Description("Enregistre ou remplace le format de briefing demandé explicitement par cet utilisateur.")]
    public Task<string> SaveAsync([Description("Préférence de présentation formulée par l’utilisateur.")] string preference,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(preference))
            throw new ArgumentException("La préférence est vide.");
        return memory.WriteConceptAsync(Concept,
            "type: Preference\ntitle: Format du briefing\ndescription: Préférence de présentation de l’utilisateur.\n",
            preference.Trim() + "\n", ct);
    }
}
