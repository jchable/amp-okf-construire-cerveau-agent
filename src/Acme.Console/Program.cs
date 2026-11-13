using System.ClientModel;
using Acme.Agent;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OKF4net.Catalog;

Console.OutputEncoding = System.Text.Encoding.UTF8;
if (args.Length > 1)
{
    Console.Error.WriteLine("Usage : Acme.Console [\"question\"]");
    return 2;
}

try
{
    var settings = DemoSettings.Load();
    using var client = DemoSettings.OpenClient();
    using var service = new AcmeAgent(settings.CatalogPath, settings.MemoryRoot);
    var agent = service.ForUser(client, settings.TenantId, settings.UserId);
    var session = await agent.CreateSessionAsync();
    var scope = new KnowledgeAccessScope(tenantId: settings.TenantId.ToString("D"), userId: settings.UserId.ToString("D"));
    var preference = Path.Combine(service.MemoryRoot, MemoryPath.For(MemoryTier.User, scope), "briefing.md");
    Console.WriteLine("Agent Acme · nouvelle session");
    if (args.Length == 1)
    {
        await AskAsync(args[0]);
        return 0;
    }

    Console.WriteLine("Pose une question ou tape /fin pour quitter.");
    while (true)
    {
        Console.Write("> ");
        var line = Console.ReadLine();
        if (line is null || line.Trim() == "/fin") break;
        if (!string.IsNullOrWhiteSpace(line)) await AskAsync(line);
    }
    return 0;

    async Task AskAsync(string question)
    {
        var response = await agent.RunAsync(question, session);
        foreach (var call in response.Messages.SelectMany(m => m.Contents.OfType<FunctionCallContent>()))
        {
            object? detail = null;
            call.Arguments?.TryGetValue("conceptId", out detail);
            Console.WriteLine($"  · {call.Name} {detail}".TrimEnd());
        }
        Console.WriteLine(response.Text);
        if (File.Exists(preference)) Console.WriteLine($"  Mémoire OKF : {preference}");
    }
}
catch (ClientResultException ex)
{
    Console.Error.WriteLine($"Appel OpenAI en échec (HTTP {ex.Status}). Vérifier URL, modèle, clé et accès réseau.");
    return 1;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}
