using Microsoft.Agents.Builder;
using Microsoft.Agents.Builder.App;
using Microsoft.Agents.Builder.State;
using Microsoft.Agents.Core.Models;

namespace Acme.Teams;

public sealed class TeamsBot : AgentApplication
{
    public TeamsBot(AgentApplicationOptions options, PersonalChat chat) : base(options)
    {
        OnActivity(ActivityTypes.Message, async (ITurnContext turn, ITurnState _, CancellationToken ct) =>
        {
            var reply = await chat.ReplyAsync(turn.Activity, ct);
            if (reply is not null)
                await turn.SendActivityAsync(reply, cancellationToken: ct);
        });
    }
}
