using Acme.Agent;
using Acme.Teams;
using Microsoft.Agents.Core.Models;

// Exercice du gestionnaire après authentification du transport, avec une activité SDK.
var settings = DemoSettings.Load();
using var service = new AcmeAgent(settings.CatalogPath, settings.MemoryRoot);
using var client = DemoSettings.OpenClient();
var chat = new PersonalChat(settings, service, client);
var activity = new Activity
{
    Type = ActivityTypes.Message,
    Id = "test-message",
    ChannelId = "msteams",
    Conversation = new ConversationAccount { Id = "personal-test", ConversationType = "personal", TenantId = settings.TenantId.ToString() },
    From = new ChannelAccount { AadObjectId = Guid.NewGuid().ToString() },
    Text = "Prépare mon briefing chiffre d’affaires pour une commande Acme livrée hier."
};
await MustIgnoreAsync();
activity.From.AadObjectId = settings.UserId.ToString();
activity.Conversation.TenantId = Guid.NewGuid().ToString();
await MustIgnoreAsync();
activity.Conversation.TenantId = settings.TenantId.ToString();
activity.Conversation.ConversationType = "channel";
await MustIgnoreAsync();
activity.Conversation.ConversationType = "personal";
var answer = await chat.ReplyAsync(activity, default);
if (string.IsNullOrWhiteSpace(answer) || await chat.ReplyAsync(activity, default) != answer)
    throw new InvalidOperationException("Le tour personnel ou sa relecture a échoué.");
Console.WriteLine(answer);

async Task MustIgnoreAsync()
{
    if (await chat.ReplyAsync(activity, default) is not null)
        throw new InvalidOperationException("L’activité hors périmètre a été acceptée.");
}
