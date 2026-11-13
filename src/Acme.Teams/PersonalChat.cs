using System.Text.Json;
using Acme.Agent;
using Microsoft.Agents.AI;
using Microsoft.Agents.Core.Models;
using Microsoft.Extensions.AI;

namespace Acme.Teams;

// Le bot de scène n’accepte qu’un compte et un chat personnel actif.
public sealed class PersonalChat(DemoSettings presenter, AcmeAgent service, IChatClient client)
{
    private readonly SemaphoreSlim turns = new(1);
    private AIAgent? agent;
    private AgentSession? session;
    private string? conversationId;
    private readonly Dictionary<string, string> replies = new();

    public async Task<string?> ReplyAsync(IActivity activity, CancellationToken ct)
    {
        var conversation = activity.Conversation;
        if (activity.ChannelId?.Channel != "msteams" || conversation?.ConversationType != "personal"
            || string.IsNullOrWhiteSpace(conversation.Id) || string.IsNullOrWhiteSpace(activity.Text)
            || !Guid.TryParse(activity.From?.AadObjectId, out var user) || user != presenter.UserId)
            return null;

        var tenantId = conversation.TenantId;
        if (activity.TryGetChannelData<JsonElement>(out var data) && data.ValueKind == JsonValueKind.Object
            && data.TryGetProperty("tenant", out var tenantData) && tenantData.ValueKind == JsonValueKind.Object
            && tenantData.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.String)
        {
            if (!Guid.TryParse(id.GetString(), out var channelTenant) || channelTenant != presenter.TenantId)
                return null;
            tenantId ??= id.GetString();
        }
        if (!Guid.TryParse(tenantId, out var tenant) || tenant != presenter.TenantId)
            return null;

        await turns.WaitAsync(ct);
        try
        {
            agent ??= service.ForUser(client, tenant, user);
            if (session is null || conversationId != conversation.Id)
            {
                session = await agent.CreateSessionAsync(ct);
                conversationId = conversation.Id;
                replies.Clear();
            }
            if (activity.Id is { Length: > 0 } key && replies.TryGetValue(key, out var previous))
                return previous;
            var reply = (await agent.RunAsync(activity.Text, session, cancellationToken: ct)).Text;
            if (!string.IsNullOrEmpty(activity.Id)) replies[activity.Id] = reply;
            return reply;
        }
        finally { turns.Release(); }
    }
}
