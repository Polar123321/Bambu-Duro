using Discord;
using Discord.Commands;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.General;

public sealed class ConfigCommand : CommandBase
{
    public ConfigCommand(
        EmbedHelper embedHelper,
        Microsoft.Extensions.Options.IOptions<ConsoleApp4.Configuration.BotConfiguration> config,
        IUserService userService,
        IGuildService guildService,
        ICommandLogService commandLogService)
        : base(embedHelper, config, userService, guildService, commandLogService)
    {
    }

    [Command("config")]
    [Summary("Abre o painel de configuracoes do servidor.")]
    [RequireUserPermission(GuildPermission.ManageGuild)]
    public async Task ConfigAsync()
    {
        await TrackUserAsync();

        if (Context.Guild == null)
        {
            await ReplyAsync("Este comando so funciona em servidores.");
            return;
        }

        var embed = EmbedHelper.CreateMajestic("Painel de Configuracoes",
                "Escolha o que deseja configurar no menu abaixo.");

        var select = new SelectMenuBuilder()
            .WithCustomId("config:select")
            .WithPlaceholder("Selecione uma opcao...")
            .WithMinValues(1)
            .WithMaxValues(1)
            .AddOption("NSFW", "nsfw", "Ativar ou desativar NSFW")
            .AddOption("NSFW - Exigir canal", "nsfw.channel", "Exigir canal marcado como NSFW")
            .AddOption("Tema - Nome", "theme.name", "Nome do tema")
            .AddOption("Tema - Footer", "theme.footer", "Rodape do tema")
            .AddOption("Tema - Cor", "theme.color", "Cor do tema")
            .AddOption("Tema - Estilo", "theme.style", "Estilo do tema")
            .AddOption("Imagem - Provider", "image.provider", "API de imagens")
            .AddOption("Boas-vindas - Titulo", "welcome.title", "Titulo do embed")
            .AddOption("Boas-vindas - Descricao", "welcome.description", "Texto do embed (use {user}, {guild}, {memberCount})")
            .AddOption("Boas-vindas - Imagem", "welcome.image", "URL da imagem (vazio para usar waifu)")
            .AddOption("Boas-vindas - Botao", "welcome.button", "Texto do botao de boas-vindas")
            .AddOption("Boas-vindas - Cor", "welcome.color", "Cor do embed (gold, blue, red...)")
            .AddOption("Boas-vindas - Usar Waifu", "welcome.waifu", "Ativar/desativar imagem aleatoria")
            .AddOption("Formulario - Enviar DM", "staff.dm.enabled", "Ativar/desativar DM ao aprovar/recusar")
            .AddOption("Formulario - DM Aprovado", "staff.dm.approved", "Mensagem DM para aprovado (use {user}, {guild})")
            .AddOption("Formulario - DM Negado", "staff.dm.denied", "Mensagem DM para negado (use {user}, {guild})");

        var components = EmbedHelper.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[] { select });
        });

        await ReplyAsync(components: components);
    }
}
