using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.General;

public sealed class ConfigInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IGuildConfigStore _store;
    private readonly EmbedHelper _embeds;

    public ConfigInteractions(IGuildConfigStore store, EmbedHelper embeds)
    {
        _store = store;
        _embeds = embeds;
    }

    [ComponentInteraction("config:select")]
    public async Task SelectAsync(string[] selections)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Este comando so funciona em servidores.", ephemeral: true);
            return;
        }

        if (Context.User is SocketGuildUser guildUser &&
            !guildUser.GuildPermissions.ManageGuild)
        {
            await RespondAsync("Permissao insuficiente.", ephemeral: true);
            return;
        }

        if (selections.Length == 0)
        {
            await RespondAsync("Selecione uma opcao.", ephemeral: true);
            return;
        }

        var key = selections[0];
        if (key == "nsfw")
        {
            var components = new ComponentBuilderV2()
                .WithActionRow(new[]
                {
                    new ButtonBuilder()
                        .WithLabel("Ativar")
                        .WithCustomId("config:nsfw:on")
                        .WithStyle(ButtonStyle.Success),
                    new ButtonBuilder()
                        .WithLabel("Desativar")
                        .WithCustomId("config:nsfw:off")
                        .WithStyle(ButtonStyle.Danger)
                })
                .Build();

            await RespondAsync("Escolha:", components: components, ephemeral: true);
            return;
        }

        if (key == "nsfw.channel")
        {
            var components = new ComponentBuilderV2()
                .WithActionRow(new[]
                {
                    new ButtonBuilder()
                        .WithLabel("Exigir canal NSFW")
                        .WithCustomId("config:nsfwchan:on")
                        .WithStyle(ButtonStyle.Success),
                    new ButtonBuilder()
                        .WithLabel("Nao exigir")
                        .WithCustomId("config:nsfwchan:off")
                        .WithStyle(ButtonStyle.Danger)
                })
                .Build();

            await RespondAsync("Escolha:", components: components, ephemeral: true);
            return;
        }

        if (key == "welcome.waifu")
        {
            var components = new ComponentBuilderV2()
                .WithActionRow(new[]
                {
                    new ButtonBuilder()
                        .WithLabel("Ativar")
                        .WithCustomId("config:welcome:waifu:on")
                        .WithStyle(ButtonStyle.Success),
                    new ButtonBuilder()
                        .WithLabel("Desativar")
                        .WithCustomId("config:welcome:waifu:off")
                        .WithStyle(ButtonStyle.Danger)
                })
                .Build();

            await RespondAsync("Escolha:", components: components, ephemeral: true);
            return;
        }

        if (key == "staff.dm.enabled")
        {
            var components = new ComponentBuilderV2()
                .WithActionRow(new[]
                {
                    new ButtonBuilder()
                        .WithLabel("Ativar")
                        .WithCustomId("config:staffdm:on")
                        .WithStyle(ButtonStyle.Success),
                    new ButtonBuilder()
                        .WithLabel("Desativar")
                        .WithCustomId("config:staffdm:off")
                        .WithStyle(ButtonStyle.Danger)
                })
                .Build();

            await RespondAsync("Escolha:", components: components, ephemeral: true);
            return;
        }

        await RespondWithModalAsync<ConfigValueModal>($"config:set:{key}");
    }

    [ComponentInteraction("config:nsfw:*")]
    public async Task ToggleNsfwAsync(string value)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        var config = await _store.GetAsync(Context.Guild.Id);
        config.NsfwEnabled = value == "on";
        await _store.SaveAsync(Context.Guild.Id, config);

        var embed = _embeds.CreateSuccess("Config atualizado", $"NSFW: {(config.NsfwEnabled ? "on" : "off")}");
        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("config:nsfwchan:*")]
    public async Task ToggleNsfwChannelAsync(string value)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        var config = await _store.GetAsync(Context.Guild.Id);
        config.RequireNsfwChannel = value == "on";
        await _store.SaveAsync(Context.Guild.Id, config);

        var embed = _embeds.CreateSuccess("Config atualizado",
                $"Exigir canal NSFW: {(config.RequireNsfwChannel ? "on" : "off")}");
        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("config:welcome:waifu:*")]
    public async Task ToggleWelcomeWaifuAsync(string value)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        var config = await _store.GetAsync(Context.Guild.Id);
        config.WelcomeUseWaifuImage = value == "on";
        await _store.SaveAsync(Context.Guild.Id, config);

        var embed = _embeds.CreateSuccess("Config atualizado",
                $"Boas-vindas (imagem waifu): {(config.WelcomeUseWaifuImage ? "on" : "off")}");
        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("config:staffdm:*")]
    public async Task ToggleStaffDmAsync(string value)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        var config = await _store.GetAsync(Context.Guild.Id);
        config.StaffDmEnabled = value == "on";
        await _store.SaveAsync(Context.Guild.Id, config);

        var embed = _embeds.CreateSuccess("Config atualizado",
                $"Formulario DM: {(config.StaffDmEnabled ? "on" : "off")}");
        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ModalInteraction("config:set:*")]
    public async Task SaveValueAsync(string key, ConfigValueModal modal)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        var config = await _store.GetAsync(Context.Guild.Id);
        var value = modal.Value.Trim();

        switch (key)
        {
            case "theme.name":
                config.ThemeName = value;
                break;
            case "theme.footer":
                config.ThemeFooter = value;
                break;
            case "theme.color":
                config.ThemeColor = value;
                break;
            case "theme.style":
                config.ThemeStyle = value;
                break;
            case "image.provider":
                config.ImageProvider = value;
                break;
            case "welcome.title":
                config.WelcomeTitle = value;
                break;
            case "welcome.description":
                config.WelcomeDescription = value;
                break;
            case "welcome.image":
                config.WelcomeImageUrl = value;
                break;
            case "welcome.button":
                config.WelcomeButtonLabel = value;
                break;
            case "welcome.color":
                config.WelcomeColor = value;
                break;
            case "staff.dm.approved":
                config.StaffDmApproved = value;
                break;
            case "staff.dm.denied":
                config.StaffDmDenied = value;
                break;
            default:
                await RespondAsync("Chave invalida.", ephemeral: true);
                return;
        }

        await _store.SaveAsync(Context.Guild.Id, config);
        var embed = _embeds.CreateSuccess("Config atualizado", $"{key} salvo.");
        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    public class ConfigValueModal : IModal
    {
        public string Title => "Configurar";

        [InputLabel("Valor")]
        [ModalTextInput("value", TextInputStyle.Short, maxLength: 500)]
        public string Value { get; set; } = string.Empty;
    }
}
