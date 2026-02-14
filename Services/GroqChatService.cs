using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Services;

public sealed class GroqChatService : IGroqChatService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const int MaxDiscordChars = 900;

    private readonly HttpClient _http;
    private readonly IOptions<GroqConfiguration> _cfg;

    public GroqChatService(HttpClient http, IOptions<GroqConfiguration> cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public async Task<string> WhatIfAsync(string scenario, string userName, CancellationToken cancellationToken = default)
    {
        scenario = (scenario ?? string.Empty).Trim();
        if (scenario.Length == 0)
        {
            return "Escreva um cenario para eu analisar. Ex: `e se todo mundo pudesse parar o tempo por 10 segundos?`";
        }

        if (scenario.Length > 1800)
        {
            scenario = scenario[..1800];
        }

        var system = """
Voce e um narrador de "what if" (e se) com raciocinio causal.
Objetivo: deduzir o que provavelmente aconteceria nessa realidade alternativa.

Regras:
- Responda em portugues-BR.
- Seja curto, direto e divertido (sem ser ofensivo).
- Nao trate como fato do mundo real: e hipotese/ficcao.
- Mostre 1a e 2a ordem (efeitos imediatos e consequencias).
- Evite instrucoes de crime/violencia. Se o cenario pedir isso, responda de forma segura e high-level.

Formato (obrigatorio):
1) 1 frase de abertura bem "chamativa"
2) 4 bullets no maximo: "Agora", "Depois", "Efeito colateral", "Plot twist"
3) 1 frase final tipo punchline/fechamento

Limites:
- No maximo ~900 caracteres.
""";

        return await CompleteAsync(
            system,
            $"Usuario: {userName}\nCenario: {scenario}",
            cancellationToken
        ).ConfigureAwait(false);
    }

    public async Task<string> MentionReplyAsync(
        string prompt,
        string userName,
        string? guildName = null,
        string? userMemoryContext = null,
        CancellationToken cancellationToken = default)
    {
        prompt = (prompt ?? string.Empty).Trim();
        if (prompt.Length == 0)
        {
            prompt = "oi";
        }

        if (prompt.Length > 1800)
        {
            prompt = prompt[..1800];
        }

        var system = """
Voce e um assistente de chat para Discord.
Objetivo: responder a mensagem de um usuario que marcou o bot.

Regras:
- Responda em portugues-BR.
- Fale como uma pessoa real: direto, natural e sem formalidade robotica.
- Seja curto, objetivo, amigavel e com humor quando combinar.
- Nao seja moralista, paternalista ou "coach".
- Nao de licao de moral.
- Use memoria do usuario para manter contexto e coerencia quando existir.
- Priorize "fatos estaveis" e contexto relevante para manter continuidade.
- So cite memoria quando for realmente relevante para a mensagem atual.
- Nao force lembrancas para saudacoes simples ou mensagens curtas.
- Em saudacao curta (ex: oi/opa), responda curto e natural.
- Nao reinicie assunto com "e ai, tudo bem?" repetidamente.
- Se ja existe conversa em andamento, continue de onde parou.
- Para mensagens curtas como "ah, sla" ou "...", responda sem resetar o contexto.
- Evite tom de terapeuta e evite perguntas em cascata.
- Em flerte, provocacao ou duplo sentido, pode entrar na brincadeira de forma leve e humana.
- Evite repeticao de bordoes.
- Evite ser prolixo; no maximo ~500 caracteres.
- Nao invente fatos quando houver incerteza.
- Nao entregue instrucoes detalhadas para crime real ou violencia real.
- Se aparecer pedido claramente criminoso/perigoso, recuse curto (sem sermao) e mude de assunto.
- Nunca ataque o usuario nem grupos protegidos.
""";

        var userContextBuilder = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(guildName))
        {
            userContextBuilder.Append("Servidor: ");
            userContextBuilder.AppendLine(guildName);
        }

        userContextBuilder.Append("Usuario: ");
        userContextBuilder.AppendLine(userName);
        if (!string.IsNullOrWhiteSpace(userMemoryContext))
        {
            userContextBuilder.AppendLine();
            userContextBuilder.AppendLine(userMemoryContext);
        }

        userContextBuilder.AppendLine();
        userContextBuilder.Append("Mensagem atual: ");
        userContextBuilder.AppendLine(prompt);
        userContextBuilder.AppendLine("Tarefa: responda como pessoa real de Discord, com continuidade, sem sermoes e sem resetar para saudacao generica.");

        var mentionTemperature = Math.Max(0.65, Math.Min(0.95, _cfg.Value.Temperature));
        var mentionMaxTokens = Math.Max(_cfg.Value.MaxTokens, 260);
        return await CompleteAsync(system, userContextBuilder.ToString(), mentionTemperature, mentionMaxTokens, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> CompleteAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken)
    {
        return await CompleteAsync(systemPrompt, userPrompt, _cfg.Value.Temperature, _cfg.Value.MaxTokens, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        double temperature,
        int maxTokens,
        CancellationToken cancellationToken)
    {
        var apiKey = _cfg.Value.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "Groq API key nao configurada. Configure `Groq:ApiKey` no appsettings.json ou via `DISCORD_BOT_Groq__ApiKey`.";
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());

        var payload = new
        {
            model = _cfg.Value.Model,
            temperature,
            max_tokens = maxTokens,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            }
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        req.Content = new StringContent(json, Encoding.UTF8, "application/json");

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);

        var body = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!resp.IsSuccessStatusCode)
        {
            var msg = body.Length > 350 ? body[..350] + "..." : body;
            return $"Falha ao chamar Groq ({(int)resp.StatusCode}): {msg}";
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            var content = root
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return string.IsNullOrWhiteSpace(content)
                ? "Nao consegui gerar uma resposta agora. Tente novamente."
                : Clamp(content.Trim());
        }
        catch
        {
            return "Resposta do Groq veio em um formato inesperado. Tente novamente.";
        }
    }

    private static string Clamp(string text)
    {
        if (text.Length <= MaxDiscordChars)
        {
            return text;
        }

        var cut = text.LastIndexOf('\n', Math.Min(MaxDiscordChars, text.Length - 1));
        if (cut < 0)
        {
            cut = text.LastIndexOf(' ', Math.Min(MaxDiscordChars, text.Length - 1));
        }

        if (cut < 0)
        {
            cut = MaxDiscordChars;
        }

        return text[..cut].TrimEnd() + "\n(...curto e grosso)";
    }
}
