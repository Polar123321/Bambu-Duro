# Bambu Duro

Bot de Discord em C# com foco em moderacao, economia, utilidades e respostas por IA, usando arquitetura modular com `Discord.Net`, `EF Core`, `Serilog` e `HostBuilder`.

## Visao geral

- Base principal em `ConsoleApp4/` com comandos prefixados e slash.
- Mais de 70 comandos distribuidos entre moderacao, diversao, utilidades, economia, RPG e perfil.
- Pipeline de interacao com confirmacao em botoes para acoes sensiveis (warn, mute, ban e similares).
- Resposta por mencao usando Groq (`GroqChatService`) com contexto de conversa.
- Memoria de usuario em banco e memoria longa em JSON por usuario.
- Execucao em modo console (`net8.0`) e modo Windows com painel (`net8.0-windows`).

## Stack

- `.NET 8`
- `Discord.Net 3.19.0-beta.1`
- `Entity Framework Core 8` + `SQLite`
- `Microsoft.Extensions.Hosting` + `DI`
- `Serilog` (console + arquivo)

## Estrutura do projeto

```text
ConsoleApp4/
  Commands/             # Comandos prefixados e modulos de interacao
  Handlers/             # Handlers de comandos, interacoes, welcome e invites
  Services/             # Regras de negocio, IA, persistencia JSON e integracoes
  Data/                 # DbContext e fabrica EF Core
  Models/               # Entidades e enums
  Configuration/        # Modelos de configuracao (Bot, Groq, Brain, Economy...)
  UI/                   # Componentes da interface Windows (net8.0-windows)
  appsettings.json      # Configuracao principal
```

## Requisitos

- SDK .NET 8 instalado
- Bot criado no Discord Developer Portal
- Intents habilitados no portal e no codigo (incluindo `Message Content` e `Server Members`)

## Configuracao rapida

1. Edite `ConsoleApp4/appsettings.json`.
2. Configure ao menos:
   `Bot.Token`, `Bot.OwnerUserId`, `Groq.ApiKey` (se usar resposta por IA).
3. Preferencialmente use variaveis de ambiente para segredos.

### Variaveis de ambiente recomendadas

```powershell
$env:DISCORD_BOT_Bot__Token="SEU_TOKEN"
$env:DISCORD_BOT_Groq__ApiKey="SUA_GROQ_KEY"
```

O projeto usa prefixo `DISCORD_BOT_`, entao `Secao:Chave` vira `DISCORD_BOT_Secao__Chave`.

## Executar

### Console (cross-platform)

```powershell
dotnet restore
dotnet run --project ConsoleApp4 --framework net8.0
```

### Windows com painel

```powershell
dotnet run --project ConsoleApp4 --framework net8.0-windows
```

## Banco de dados e persistencia

- Banco SQLite (EF Core): por padrao em `%LOCALAPPDATA%/ConsoleApp4/bot.db`.
- Migrations aplicadas automaticamente no startup quando existirem.
- Schema complementar para warns/memoria e garantido no bootstrap.
- Memoria longa JSON por usuario: `%LOCALAPPDATA%/ConsoleApp4/long-memory/*.json`.

### Comandos uteis de migration

```powershell
dotnet tool install --global dotnet-ef
dotnet ef migrations add NomeDaMigration --project ConsoleApp4
dotnet ef database update --project ConsoleApp4
```

## IA por mencao

Quando alguem menciona o bot, ele pode responder via Groq (`GroqChatService`) usando:

- prompt atual do usuario
- contexto do servidor/usuario
- memoria curta e longa quando habilitadas

Configuracao relevante em `appsettings.json`:

- `Groq.ApiKey`
- `Groq.Model`
- `Groq.Temperature`
- `Groq.MaxTokens`
- `Brain.*` para comportamento de memoria

## Principais modulos de comando

- `Moderation`: warn, mute, kick, ban, clear, staff tools e revisao
- `Economy`: daily, work, crime, shop, inventory, buy/sell/use, marriage
- `Fun`: acoes, coin, dice, ship, what-if
- `User`: avatar, profile, activity
- `General`: ajuda, info, ping, config, imagem, formulario e utilitarios
- `Rpg`: hunt, stats

## Logs e observabilidade

- Console com Serilog
- Arquivos em `logs/bot-*.log`
- Captura de falhas nao tratadas em `crash.log`

## Seguranca

- Nao commite token/API key.
- Use variaveis de ambiente para credenciais.
- Restrinja permissoes do bot no Discord ao necessario.
- Revogue e regenere token imediatamente se exposto.

## Troubleshooting rapido

- Bot online mas sem responder comandos:
  confirme intents no portal e permissoes no servidor.
- Slash commands nao aparecem:
  aguarde propagacao global e verifique logs de `InteractionHandler`.
- Erro de banco:
  valide `Database.ConnectionString` e permissoes de escrita no diretorio de dados.
- Falhas da IA:
  revise `Groq.ApiKey`, modelo e limite de tokens.
