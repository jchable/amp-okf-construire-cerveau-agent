using Acme.Agent;
using Acme.Teams;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Storage;

var builder = WebApplication.CreateBuilder(args);
var presenter = DemoSettings.Load();
var botId = DemoSettings.ReadId("ACME_BOT_ID").ToString("D");
var config = builder.Configuration;
config["TokenValidation:Audiences:0"] = botId;
config["TokenValidation:TenantId"] = presenter.TenantId.ToString("D");
config["TokenValidation:AzureBotServiceOnly"] = "true";
config["OutboundHostValidator:Enabled"] = "true";
config["OutboundHostValidator:IncludeDefaultMicrosoftHosts"] = "true";
config["ConnectionsMap:0:ServiceUrl"] = "*";
config["ConnectionsMap:0:Connection"] = "ServiceConnection";
var connection = config.GetSection("Connections:ServiceConnection:Settings");
connection["AuthType"] = "ClientSecret";
connection["AuthorityEndpoint"] = $"https://login.microsoftonline.com/{presenter.TenantId:D}";
connection["ClientId"] = botId;
connection["ClientSecret"] = DemoSettings.Required("ACME_BOT_SECRET");
connection["Scopes:0"] = "https://api.botframework.com/.default";

builder.Services.AddSingleton(presenter);
builder.Services.AddSingleton(_ => DemoSettings.OpenClient());
builder.Services.AddSingleton(_ => new AcmeAgent(presenter.CatalogPath, presenter.MemoryRoot));
builder.Services.AddSingleton<PersonalChat>();
builder.Services.AddSingleton<IStorage, MemoryStorage>();
builder.AddAgentDefaults().AddAgent<TeamsBot>()
    .AddAgentAuthorization(host => host.AddAgentAspNetAuthentication(), forceEnable: true);

var app = builder.Build();
app.UseAgents();
app.MapDefaultAgentEndpoints();
app.MapGet("/privacy", () => Results.Text("Démonstration Acme réservée au présentateur. Les questions, les extraits Acme et la préférence de briefing sont transmis au service OpenAI configuré. La préférence est conservée sur le poste sous le profil du présentateur. Le chat suit les règles de conservation du tenant Teams."));
app.MapGet("/terms", () => Results.Text("Démonstration aMP Lyon sur des données fictives Acme. Usage de présentation en chat personnel par le présentateur."));
app.Run();
