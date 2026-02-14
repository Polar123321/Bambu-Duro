using Discord;
using Discord.Interactions;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services;
using ConsoleApp4.Services.Interfaces;

namespace ConsoleApp4.Commands.Fun;

public sealed class ActionInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly WaifuPicsClient _waifu;
    private readonly IGuildConfigStore _configStore;
    private readonly IMarriageStore _marriageStore;
    private readonly EmbedHelper _embeds;

    public ActionInteractions(
        WaifuPicsClient waifu,
        IGuildConfigStore configStore,
        IMarriageStore marriageStore,
        EmbedHelper embeds)
    {
        _waifu = waifu;
        _configStore = configStore;
        _marriageStore = marriageStore;
        _embeds = embeds;
    }

    [ComponentInteraction("act:ret:*:*:*")]
    public async Task RetribuirAsync(string category, string expectedUserIdRaw, string partnerUserIdRaw)
    {
        if (!TryGetTitle(category, out var title))
        {
            await RespondAsync("Acao invalida.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(expectedUserIdRaw, out var expectedUserId) ||
            !ulong.TryParse(partnerUserIdRaw, out var partnerUserId))
        {
            await RespondAsync("Dados invalidos no botao.", ephemeral: true);
            return;
        }

        if (Context.User.Id != expectedUserId)
        {
            await RespondAsync("Somente a pessoa marcada pode retribuir essa acao.", ephemeral: true);
            return;
        }

        if (Context.Guild == null)
        {
            await RespondAsync("Este botao so funciona em servidores.", ephemeral: true);
            return;
        }

        await DeferAsync();

        var useNsfw = category == "kiss" && await IsNsfwAllowedAsync();
        var imageUrl = await _waifu.GetImageUrlAsync(category, nsfw: useNsfw);
        if (string.IsNullOrWhiteSpace(imageUrl) && useNsfw)
        {
            imageUrl = await _waifu.GetImageUrlAsync(category, nsfw: false);
        }

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            await FollowupAsync("Nao consegui buscar uma imagem agora.", ephemeral: true);
            return;
        }

        var description = $"{Context.User.Mention} retribuiu {title.ToLowerInvariant()} em <@{partnerUserId}>.";
        var embed = _embeds.CreateMajesticWithImage($"{title} Retribuido", description, imageUrl);

        if (category == "kiss" && await AreMarriedAsync(Context.User.Id, partnerUserId))
        {
            embed.AddField("💍 Destino selado", "Beijo retribuido entre casados. O servidor acabou de presenciar um momento lendario.", false);
        }

        var components = _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Retribuir")
                    .WithStyle(ButtonStyle.Primary)
                    .WithCustomId($"act:ret:{category}:{partnerUserId}:{Context.User.Id}")
            });
        });

        await FollowupAsync(components: components);
    }

    private async Task<bool> IsNsfwAllowedAsync()
    {
        if (Context.Guild == null)
        {
            return false;
        }

        var config = await _configStore.GetAsync(Context.Guild.Id);
        if (!config.NsfwEnabled)
        {
            return false;
        }

        if (config.RequireNsfwChannel &&
            Context.Channel is ITextChannel textChannel && !textChannel.IsNsfw)
        {
            return false;
        }

        return true;
    }

    private async Task<bool> AreMarriedAsync(ulong userIdA, ulong userIdB)
    {
        if (Context.Guild == null)
        {
            return false;
        }

        var id1 = Math.Min(userIdA, userIdB);
        var id2 = Math.Max(userIdA, userIdB);
        var records = await _marriageStore.GetAsync(Context.Guild.Id);
        return records.Any(r => r.UserId1 == id1 && r.UserId2 == id2);
    }

    private static bool TryGetTitle(string category, out string title)
    {
        title = category switch
        {
            "hug" => "🤗 Abraco",
            "kiss" => "💋 Beijo",
            "pat" => "🫳 Cafune",
            "cuddle" => "🫶 Aconchego",
            "slap" => "✋ Tapa",
            "wink" => "😉 Piscadinha",
            "wave" => "👋 Tchau",
            _ => string.Empty
        };

        return title.Length > 0;
    }
}
