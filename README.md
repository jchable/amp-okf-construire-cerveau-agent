# Du code C# à un agent dans Teams — aMP Lyon 2026

**Plan d’implémentation et conducteur.** En 24 minutes, un public principalement Microsoft 365 voit une règle métier devenir consultable, un agent l’utiliser, puis Teams retrouver une préférence donnée en console. Tout le code et les commandes sont préparés avant la session. Les trois démos sont implémentées pour Windows x64. Le modèle OpenAI du présentateur et le chat du tenant restent à valider en répétition.

**Question à garder à l’écran** — « Une commande Acme livrée hier entre-t-elle déjà dans le chiffre d’affaires ? » La politique Acme exige que la commande soit livrée et que le délai de retour de 30 jours soit écoulé. La réponse attendue est donc « non », avec le concept `policies/revenue-recognition` pour source. Le présentateur peut ouvrir sa copie rendue en HTML. [Politique Acme de référence](https://github.com/jchable/okf4net/blob/521ea0cecf543ac73b5dce4f12c84b945e06a5a8/bundles/acme_retail/policies/revenue-recognition.md)

[Support PowerPoint de la session](<slides/aMP-Lyon-Construire le cerveau de vos agents IA v2.pptx>)

## Première recette sur le poste

Ouvrir **PowerShell 7 dans VS Code**. Avant chaque répétition ou passage sur scène, arrêter la console et l’hôte Teams du passage précédent, puis lancer la préparation 1 une fois. Les préparations 2 et 3 vérifient et compilent leurs hôtes en conservant la mémoire. Enchaîner les démos 1, 2 et 3 dans le même terminal. Avant de saisir les clés, le contrôle local de la démo 2 permet de tester le parcours console → gestionnaire Teams avec des identités fictives.

## Lancer la démo 1

Le petit programme `Acme.Orders` exécute une règle écrite en C# et affiche le résultat pour deux commandes, livrées depuis 1 et 31 jours. Sa méthode et ses commentaires XML servent ensuite de sources à `okfgen`. Le programme lui-même ne charge aucun bundle OKF.

| Étape | Ce qui est exécuté ou lu |
|---|---|
| `dotnet run` | Exécute le code métier C# pour illustrer la question Acme. |
| `okfgen generate` | Analyse les sources C# et ajoute leurs concepts au bundle métier Acme copié pendant la préparation. |
| `Get-Content` | Affiche la fiche Markdown de la politique Acme pour montrer un concept OKF directement. Cette fiche métier est fournie avec le corpus initial. |
| `okf validate` | Lit le bundle enrichi et vérifie sa conformité au format OKF. |
| `okf-render` | Lit le même bundle pour construire le site HTML et son graphe. |
| Navigation dans le site | Permet de lire les concepts et de suivre leurs relations. L’agent de la démo 2 utilisera ce même bundle comme connaissance. |

La génération depuis le code documente les types, les méthodes et leurs commentaires. La politique métier des 30 jours est déjà présente dans le corpus Acme initial.

**Avant la séance · préparer le poste.** Exécuter les deux commandes ci-dessous avant chaque passage. La préparation utilise Git, tar, le SDK .NET 10 et les paquets NuGet pour Windows x64. Elle remet en place le bundle métier initial et son catalogue, efface le rendu Acme précédent et réinitialise la mémoire de la console et de Teams. Le grand rendu OKF4net reste disponible.

```powershell
Set-Location 'E:\Sources\amp-okf-construire-cerveau-agent'
./scripts/Prepare-Demo1.ps1 -OkfRepository 'E:\Sources\okf4net'
```

**Avant la séance · préparer le terminal.** Coller ce bloc une fois dans la fenêtre PowerShell qui servira sur scène. Les trois noms désignent les exécutables locaux pour la durée de cette session.

```powershell
Set-Location "$env:TEMP/amp-lyon-2026-demo1"
Set-Alias okfgen    "$PWD/tools/okfgen/OkfProducer.Cli.exe"
Set-Alias okf       "$PWD/tools/okf/okf.exe"
Set-Alias okf-render "$PWD/tools/okf-render/okf-render.exe"
```

**Grand rendu automatique.** La préparation 1 vérifie la présence de `index.html` et `graph.html` dans le grand site. Elle réutilise le rendu existant. Si une page manque ou si une génération précédente a été interrompue, elle génère le bundle depuis la copie figée d’OKF4net, le valide, puis lance `okf-render`. Prévoir plusieurs minutes lors du premier passage. Attendre « Démo 1 prête » avant de continuer.

**Sur scène · Acme.** Dans le terminal PowerShell 7 préparé ci-dessus, coller les lignes une par une. La première exécute les deux exemples de commandes clients. Après la génération, lire la politique affichée par `Get-Content`, puis lancer la validation. Attendre « 14 concept(s) » et « 0 error(s) » avant le rendu. Les 27 avertissements sont attendus.

```powershell
dotnet run --project ./src/Acme.Orders -c Release --no-build
okfgen generate --repo ./src/Acme.Orders --out ./artifacts/acme/bundle --update
Get-Content ./artifacts/acme/bundle/policies/revenue-recognition.md
okf validate ./artifacts/acme/bundle --as-of 2026-11-12
okf-render ./artifacts/acme/bundle --out ./artifacts/acme/site
Invoke-Item ./artifacts/acme/site/index.html
```

**Sur scène · grand format.** Ouvrir le graphe OKF4net préparé en répétition.

```powershell
Invoke-Item ./artifacts/okf4net/site/graph.html
```

**Parcours dans le navigateur.** Dans Acme, rechercher `IsRevenueRecognized` pour ouvrir la méthode projetée depuis C#. Ouvrir ensuite la fiche `metrics/revenue` et suivre le lien vers `policies/revenue-recognition` pour lire la règle des 30 jours. Dans le grand site, partir du graphe global et chercher `OkfBundleTools`. Cette classe sera le point de départ de la démo agent.

| Répétition du 12 novembre 2026 | Concepts validés | Erreurs | Avertissements |
|---|---:|---:|---:|
| Acme, connaissance métier et code | 14 | 0 | 27 |
| Sources publiques de la copie OKF4net | 1 056 | 0 | 24 |

Les sources, les outils et les résultats résident sous `%TEMP%\amp-lyon-2026-demo1`, hors du dossier synchronisé par Dropbox. Conserver `artifacts\acme\bundle` pour l’agent. Le navigateur lit les HTML et les graphes sur disque. Un éventuel tunnel VS Code concernera uniquement Teams.

## Lancer la démo 2 · Agent Acme en console

**Configurer puis préparer.** Revenir au dépôt dans le même terminal. Renseigner les variables ci-dessous une fois avant la répétition. Utiliser l’URL de base du service Chat Completions (par exemple `https://api.openai.com/v1`), un modèle acceptant les appels d’outils et la clé associée. Les GUID sont ceux du tenant et du compte présentateur.

```powershell
Set-Location 'E:\Sources\amp-okf-construire-cerveau-agent'
$env:OKF_CHAT_BASE_URL = Read-Host "URL de base OpenAI, avec /v1 si nécessaire"
$env:OKF_CHAT_MODEL = Read-Host "Nom du modèle sur cet endpoint"
$env:OKF_CHAT_API_KEY = Read-Host "Clé de cet endpoint" -MaskInput
$env:ACME_TENANT_ID = Read-Host "Directory (tenant) ID Entra"
$env:ACME_USER_ID = Read-Host "Object ID Entra de ton compte utilisateur"
./scripts/Prepare-Demo2.ps1
```

La préparation vérifie les variables et les GUID, ouvre le catalogue installé en démo 1, valide le bundle puis compile la console. Attendre « Démo 2 prête ». Les contrôles de configuration sont locaux. Le premier échange avec le modèle vérifie réellement la clé et le nom du modèle. Les deux hôtes utilisent le dossier fixe `%TEMP%\amp-lyon-2026-demo1\memory`. Le script affiche le catalogue, le bundle et la mémoire sélectionnés. Pour relancer la préparation avec les variables déjà saisies, exécuter uniquement `./scripts/Prepare-Demo2.ps1`.

**Sur scène.** Lancer la console puis envoyer les messages ci-dessous un par un à l’invite `>`. Attendre la réponse et le retour de l’invite entre deux messages. Les lignes précédées d’un point indiquent les outils appelés. La source attendue est `policies/revenue-recognition`. Après l’écriture, la console affiche le chemin du concept de mémoire à ouvrir.

```powershell
dotnet run --project ./src/Acme.Console -c Release --no-build
```

```text
Une commande Acme livrée hier entre-t-elle déjà dans le chiffre d’affaires ? Cite ta source.
Mémorise ma préférence : pour mes briefings chiffre d’affaires, commence par « Point du matin », puis donne la règle et un exemple.
/fin
```

**Vérifier le rappel.** Après `/fin`, le terminal doit afficher à nouveau l’invite PowerShell. Vérifier que la console a affiché `memoriser_preference` puis le chemin de `briefing.md`. Cette commande ouvre un nouveau processus avec une session vide. La question omet la formule mémorisée.

```powershell
dotnet run --project ./src/Acme.Console -c Release --no-build -- "Prépare mon briefing chiffre d’affaires pour une commande Acme livrée hier."
```

**Passage à Teams.** `Acme.Agent` contient le client et la fabrique de l’agent. L’hôte Teams ouvre le même catalogue et le même répertoire de mémoire, puis appelle `ForUser(client, tenantId, userId)` avec l’identité issue de l’activité authentifiée. Les GUID sont normalisés dans les deux hôtes. Le concept `briefing.md` est rangé dans la portée OKF du compte, dans le dossier de mémoire commun. Une nouvelle préférence remplace la précédente. Seule la préférence demandée est enregistrée. Les outils de connaissance sont en lecture seule. Passer directement à Teams après la console. La préparation 3 conserve la préférence. La préparation 1 et le nettoyage final réinitialisent le passage.

## Lancer la démo 3 · chat personnel Teams

**Avant la recette.** Utiliser une application Entra mono-tenant dédiée et une ressource Azure Bot associée au même client ID, avec le canal Microsoft Teams activé. Pour cet hôte local, le mécanisme de connexion sortante est le secret client. Dans le terminal de la démo 2, conserver les variables OpenAI, tenant et utilisateur. Depuis la racine du dépôt, préparer l’hôte.

```powershell
Set-Location 'E:\Sources\amp-okf-construire-cerveau-agent'
$env:ACME_BOT_ID = Read-Host "Application (client) ID du bot"
$env:ACME_BOT_SECRET = Read-Host "Valeur du secret client du bot" -MaskInput
```

Dans VS Code, ouvrir **Ports → Forward a Port**, saisir **3978** et choisir la visibilité publique pour que le service Azure Bot puisse joindre le poste. L’endpoint applicatif vérifie le JWT du canal, y compris en développement. Copier l’URL HTTPS transférée, puis préparer le ZIP et démarrer le service.

```powershell
$tunnel = Read-Host "URL HTTPS du port 3978 depuis VS Code"
./scripts/Prepare-Demo3.ps1 -PublicUrl $tunnel
```

Attendre « Démo 3 prête », puis démarrer le service.

```powershell
dotnet run --project ./src/Acme.Teams -c Release --no-build -- --urls http://127.0.0.1:3978
```
 La préparation vérifie les variables OpenAI et Entra, valide le même bundle, compile Teams et génère le ZIP avec l’URL du tunnel. Elle utilise la mémoire affichée en démo 2 et peut être relancée après la mémorisation. Pour actualiser le ZIP après un changement de tunnel, relancer `Prepare-Demo3.ps1` avec la nouvelle URL.

Laisser ce terminal occupé par l’hôte. Dans un second terminal PowerShell, les deux lignes suivantes doivent afficher respectivement `200` et `401`. Le second statut confirme que la route du bot exige un jeton.

```powershell
(Invoke-WebRequest http://127.0.0.1:3978/privacy).StatusCode
(Invoke-WebRequest http://127.0.0.1:3978/api/messages -Method Post -ContentType application/json -Body '{}' -SkipHttpErrorCheck).StatusCode
```

Dans **Azure Bot → Configuration → Messaging endpoint**, enregistrer l’URL du tunnel suivie de `/api/messages`. Ouvrir aussi l’URL publique du tunnel suivie de `/privacy` depuis une fenêtre privée du navigateur. Le texte Acme doit s’afficher sans connexion à VS Code. Dans Teams, importer le ZIP régénéré avec le vrai client ID, `E:\Sources\amp-okf-construire-cerveau-agent\artifacts\teams\Acme.Teams.zip`, comme application personnalisée pour son compte. Garder l’hôte et le tunnel ouverts. Si l’adresse change, actualiser l’endpoint et régénérer le ZIP pour mettre à jour les liens de l’application. [Déploiement Microsoft](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/deploy-azure-bot-service-manually)

**Sur scène.** Après la mémorisation en console, coller dans le chat personnel de l’application :

```text
Prépare mon briefing chiffre d’affaires pour une commande Acme livrée hier.
```

Attendre le format choisi en console, la règle des 30 jours et la référence `policies/revenue-recognition`. Dans le navigateur du présentateur, ouvrir la fiche rendue en démo 1. Enchaîner avec le [prompt de création](#prompt-à-projeter), le [gestionnaire de messages](src/Acme.Teams/TeamsBot.cs) et le [passage à l’agent commun](src/Acme.Teams/PersonalChat.cs). L’hôte maintient la session du chat en mémoire le temps de la démonstration. La préférence OKF persiste après redémarrage.

**Frontière de la recette.** Les contrôles locaux utilisent un endpoint OpenAI simulé et des identités fictives. La réponse libre du modèle choisi, l’authentification avec l’identité réelle du bot, la livraison et la réponse dans le tenant seront vérifiées ensemble sur le poste. Le composant de validation JWT Microsoft est fourni avec sa [licence](src/Acme.Teams/LICENSE.Microsoft.txt). [Exemple Microsoft](https://github.com/microsoft/Agents-for-net/blob/main/src/samples/Shared/AspNetExtensions.cs)

## Contrôle local des préparations et du rappel

À exécuter après la démo 1, avant la configuration réelle du bot, avec Node.js installé. Le test utilise un endpoint OpenAI simulé, des identités fictives et un dossier de mémoire isolé. Il exécute la préparation 2, enregistre une préférence avec la console, relance les préparations 2 et 3 deux fois, compare les fichiers de mémoire puis vérifie leur rappel en console et par le gestionnaire Teams.

```powershell
Set-Location 'E:\Sources\amp-okf-construire-cerveau-agent'
dotnet build ./src/Acme.Console -c Release
dotnet build ./tests/TeamsTurn -c Release
node ./tests/check-demos.mjs --prepare
```

Attendre les lignes « OK : préparations répétées, mémoire conservée. » puis « OK : politique Acme, remplacement, rappel console et Teams, isolation entre comptes. ». Ce contrôle produit un ZIP Teams avec une identité fictive. La préparation 3 du parcours réel le régénère avec le client ID du bot et le tunnel de présentation.

## Nettoyage après le passage

Quitter la console avec `/fin`, arrêter l’hôte Teams avec `Ctrl+C`, puis exécuter depuis le dépôt :

```powershell
Set-Location 'E:\Sources\amp-okf-construire-cerveau-agent'
./scripts/Clear-Demo.ps1
```

La commande efface les sorties Acme et la mémoire locale du présentateur. Elle peut être relancée sur un poste déjà nettoyé. Les outils, la copie figée et le grand site OKF4net restent prêts pour le prochain passage. Le nettoyage est local au poste. L’historique du chat Teams reste dans le tenant.

## Conducteur de scène

| Durée | Manipulation préparée | Ce que le public doit voir |
|---|---|---|
| **Démo 1 · 0–2 min** | Montrer la question, la fiche de politique Acme et la méthode documentée du petit projet C# qui applique la règle. | Une règle que chacun peut vérifier et deux commandes fictives, livrées depuis 1 et 31 jours. |
| **2–6 min** | Enrichir la copie Acme avec `okfgen`, valider, lancer `okf-render`. Ouvrir la fiche générée de la méthode, puis suivre dans le graphe métier le lien de la métrique Revenue à sa politique. | Le concept C# vient d’être généré. La politique et la métrique étaient déjà présentes dans Acme. L’index expose les deux familles. |
| **6–9 min** | Montrer la même génération et le rendu sur une copie figée du code source complet d’OKF4net. Ouvrir le graphe global puis zoomer sur `OkfBundleTools`. | Le public retrouve le même geste sur une base plus grande. Revenir à l’onglet Acme pour passer à l’agent. |
| **Démo 2 · 0–4 min** | Poser la question en console. Afficher les appels OKF, l’identifiant lu et la réponse. Ouvrir le court extrait C# qui enregistre outils et fournisseur de contexte. | La réponse applique les deux conditions et cite la fiche vue pendant la première démo. Préparer une instruction demandant la lecture explicite de la source avant citation. |
| **4–7 min** | Dire « Pour mes briefings chiffre d’affaires, commence par “Point du matin”, puis donne la règle et un exemple. » Montrer l’entrée OKF écrite et fermer la console. | Une préférence observable figure dans le stockage de l’utilisateur. La phrase distinctive rend son rappel vérifiable. |
| **Démo 3 · 0–3 min** | Dans le chat personnel Teams, envoyer « Prépare mon briefing chiffre d’affaires pour une commande Acme livrée hier. » Partir d’une session Agent Framework neuve. | La réponse commence par « Point du matin », applique la règle et cite le concept. La question Teams ne contient ni le format ni la formule mémorisée. |
| **3–8 min** | Ouvrir la source, puis montrer côte à côte le prompt de création de l’adaptateur et le gestionnaire C# préparé. Suivre le trajet du message jusqu’à l’appel à l’agent commun. | Le public peut repérer le code qu’il lui faudrait écrire, ou demander à une IA de produire, pour proposer son propre agent dans Teams. |

## Architecture et dépendances

| Élément à préparer | Rôle |
|---|---|
| `src/Acme.Orders` | Petit projet .NET avec une méthode publique documentée en XML et deux exemples datés reproductibles. La règle du projet doit concorder avec la politique Acme. |
| `artifacts/acme/bundle` et `artifacts/acme/site` | Copie enrichie du bundle et rendu correspondant. Console et Teams reçoivent exactement la même racine de connaissance. Le modèle lit via `OkfBundleTools.GetTools(OkfToolMode.ReadOnly)`. |
| `artifacts/okf4net/bundle` et `artifacts/okf4net/site` | Sorties indépendantes, réservées à la vue grand format de la démo 1. |
| `src/Acme.Agent` | Bibliothèque partagée. Construit l’`IChatClient` OpenAI, les outils et le `ChatClientAgentOptions`, ajoute le `OkfContextProvider` et crée l’`AIAgent` avec `AsAIAgent`. |
| `src/Acme.Console` et `src/Acme.Teams` | La console lance les invites préparées. L’hôte ASP.NET Core reçoit les activités du Microsoft 365 Agents SDK, résout l’identité du présentateur et appelle le même agent. Chaque hôte crée sa propre session. |
| Catalogue et mémoire | Une source `role: knowledge` pointe sur le bundle Acme. Un `FileMemoryStore` utilise le niveau `User` dans un répertoire persistant privé, hors Git et hors des sorties de rendu. Les deux processus ouvrent le même stockage. |

**Référence technique.** Figer les outils et exemples sur la révision OKF4net `521ea0cecf543ac73b5dce4f12c84b945e06a5a8` inspectée pour ce plan. Les projets ciblent .NET 10. Le poste dispose du SDK 10.0.115. La référence Agents utilise `Microsoft.Agents.AI` 1.28.0 et l’exemple OpenAI utilise `Microsoft.Extensions.AI.OpenAI` 10.8.3. Teams utilise `Microsoft.Agents.Hosting.AspNetCore` et `Microsoft.Agents.Authentication.Msal` 1.9.205. [Projet Agents](https://github.com/jchable/okf4net/blob/521ea0cecf543ac73b5dce4f12c84b945e06a5a8/src/OKF4net.Agents/OKF4net.Agents.csproj) · [Exemple de console OpenAI](https://github.com/jchable/okf4net/tree/521ea0cecf543ac73b5dce4f12c84b945e06a5a8/samples/acme-retail-agent) · [Démarrage C# du SDK M365](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/quickstart?pivots=dotnet)

## Ordre d’implémentation

1. **Préparer les outils locaux.** Le lanceur exporte la révision choisie, restaure les paquets NuGet et compile les outils ainsi que le projet Acme. La copie Acme est accompagnée de la licence et de la notice d’attribution d’origine.
2. **Générer les deux rendus.** Le lanceur recharge le corpus Acme et ajoute les concepts C# avec `--update`. Pour la vue grand format, il scanne la copie complète avec `--no-msbuild` et utilise la résolution des appels par nom. Le graphe présente les types et membres publics, avec leurs liens de contenance. Ce mode omet les liens projet → espace de noms et les appels ambigus. Chaque génération doit passer la validation avant le rendu. [Options du producteur](https://github.com/jchable/okf4net/blob/521ea0cecf543ac73b5dce4f12c84b945e06a5a8/producers/README.md)
3. **Câbler la connaissance et la mémoire en console.** Le catalogue et le `FileMemoryStore` fournissent le contexte OKF. Le modèle lit les concepts avec les outils du bundle en lecture seule. Deux fonctions limitées au compte courant lui permettent de mémoriser ou de relire la préférence de briefing. Les commandes et les variables sont décrites dans « Lancer la démo 2 ».
4. **Raccorder Teams au même utilisateur.** Pour la scène, exécuter l’hôte sur le poste. Si Teams requiert un point d’entrée HTTPS, ouvrir le tunnel depuis VS Code. La console et le bot peuvent alors ouvrir le même répertoire privé avec des chemins absolus. Dans la console de scène, configurer localement le tenant et l’Object ID Entra du présentateur. Dans Teams, dériver les mêmes valeurs de l’activité authentifiée et les normaliser de façon identique. Autoriser uniquement cette paire avant l’appel au modèle ou à la mémoire. Utiliser une nouvelle session Agent Framework pour le premier tour Teams. La clé de conversation sert au dialogue Teams, la paire tenant/utilisateur sert au rappel intersession.
5. **Préparer le prompt et l’extrait à projeter.** Utiliser le prompt ci-dessous pour préparer l’adaptateur. Garder dans le dépôt final le prompt effectivement utilisé et le code compilé correspondant. Le gestionnaire à projeter tient dans un écran. Il lit le texte et l’identité, appelle le service partagé puis envoie la réponse. Dans la version de scène, afficher le lien vers le concept rendu si le présentateur peut ouvrir son URL, sinon afficher l’identifiant et ouvrir la fiche HTML déjà prête.

   <a id="prompt-à-projeter"></a>

   > Ajoute à cette solution .NET un hôte ASP.NET Core pour un bot Teams personnel avec Microsoft 365 Agents SDK. Réutilise le service de conversation d’Acme.Agent. Route une activité Message reçue sur /api/messages vers ce service. Une fois l’activité authentifiée par le SDK, vérifie que le tenant et l’utilisateur Entra sont ceux du présentateur configuré. Passe leur identité au service pour retrouver sa mémoire OKF. Ouvre une session au premier message et conserve-la pour la suite de ce chat. Renvoie le texte et la source fournis par l’agent. Montre le gestionnaire C# et le câblage de l’hôte.

6. **Répéter le passage complet.** Partir d’une mémoire de répétition vide. Poser d’abord la question Teams témoin dans une session neuve et vérifier l’absence de « Point du matin ». Enregistrer en console, vérifier l’écriture OKF, quitter la console, repartir en session Teams neuve, puis constater le rappel depuis le même stockage. Vérifier que le marqueur de préférence ne figure ni dans le prompt système ni dans les instructions Teams. Refaire une fois avec un autre identifiant synthétique dans le test de l’hôte pour vérifier qu’il ne reçoit pas la mémoire. Chronométrer les trois séquences avec le poste et la connexion de scène.

## Vérifications sur le tenant de production

À effectuer avec le compte du présentateur avant la répétition. Ce tableau décrit les contrôles à faire. Le tenant n’a pas été inspecté.

| À vérifier | Critère pour passer en répétition |
|---|---|
| **Compte et installation personnelle** | Le compte a accès à Teams sur le bon tenant. L’import d’apps personnalisées est autorisé pour ce compte par les réglages d’organisation et la stratégie attribuée. Essayer l’installation personnelle depuis Teams. Si l’administrateur doit distribuer l’app, lui demander une disponibilité limitée au présentateur. [Stratégies Teams](https://learn.microsoft.com/en-us/microsoftteams/teams-custom-app-policies-and-settings) |
| **Identité du bot et authentification** | Une identité d’application du tenant est associée à la ressource Azure Bot. Confirmer le tenant ID, le client ID et le mécanisme d’authentification réellement utilisable par l’hôte. Vérifier sa validité jusqu’au jour J. Les identifiants du présentateur sont distincts de l’identité du bot. Les valeurs de production restent dans la configuration privée. [Configuration Azure Bot](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/deploy-azure-bot-service-manually) |
| **Transport et canal** | Le canal Microsoft Teams est activé dans Azure Bot. Le point de terminaison déclaré est l’URL HTTPS active terminée par `/api/messages`. Vérifier le tunnel ou l’hébergement depuis le réseau de présentation. Le test décisif est un message réel dans Teams qui atteint le gestionnaire authentifié et obtient une réponse. |
| **Package et audience** | Le ZIP contient `manifest.json`, `color.png` et `outline.png` à sa racine. Le `botId` correspond au client ID du bot et `bots[].scopes` contient uniquement `personal`. Vérifier l’installation sur le seul compte voulu. Le scope personnel décrit le type de chat. La restriction d’audience est appliquée par la distribution et par la vérification du couple tenant/utilisateur dans l’hôte. [Package et publication](https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/deploy-azure-bot-service-manually) |
| **Accès au modèle et mémoire** | Le poste ou l’hôte atteint l’endpoint OpenAI avec le modèle et la clé de scène. Les deux processus lisent le même bundle et peuvent accéder au même répertoire privé de mémoire après redémarrage. Une activité privée du présentateur fournit le tenant et l’Object ID attendus. Une identité manquante ou différente arrête le traitement avant la lecture de la mémoire. |
| **Accès du public** | Depuis le compte du présentateur, ouvrir la fiche source à l’écran. Sur le dépôt de conférence, publier uniquement le corpus de démonstration, le code, le prompt et la configuration d’exemple. Garder les échanges capturés, les identifiants du compte et les secrets hors du dépôt. Les participants pourront reproduire le projet avec leur propre configuration. |
