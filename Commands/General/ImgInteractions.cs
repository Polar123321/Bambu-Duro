using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;

namespace ConsoleApp4.Commands.General;

public sealed class ImgInteractions : InteractionModuleBase<SocketInteractionContext>
{
    public ImgInteractions(EmbedHelper embeds)
    {
        ImgCommand.ImgSessionStore.Configure(embeds);
    }

    [ComponentInteraction("img:prev:*:*")]
    public async Task PrevAsync(string token, string currentPageRaw)
    {
        await ChangePageAsync(token, currentPageRaw, delta: -1);
    }

    [ComponentInteraction("img:next:*:*")]
    public async Task NextAsync(string token, string currentPageRaw)
    {
        await ChangePageAsync(token, currentPageRaw, delta: 1);
    }

    private async Task ChangePageAsync(string token, string currentPageRaw, int delta)
    {
        if (Context.Interaction is not SocketMessageComponent component)
        {
            await RespondAsync("Nao consegui atualizar a imagem.", ephemeral: true);
            return;
        }

        if (!int.TryParse(currentPageRaw, out var currentPage))
        {
            await RespondAsync("Pagina invalida.", ephemeral: true);
            return;
        }

        if (!ImgCommand.ImgSessionStore.TryGet(token, out var session))
        {
            await RespondAsync("Essa sessao expirou. Rode *img novamente.", ephemeral: true);
            return;
        }

        if (session.OwnerUserId != Context.User.Id)
        {
            await RespondAsync("Somente quem executou o comando pode trocar a pagina.", ephemeral: true);
            return;
        }

        var nextPage = Math.Clamp(currentPage + delta, 1, Math.Max(1, session.Results.Count));
        var components = ImgCommand.BuildComponents(token, session.OwnerUserId, nextPage, session.Query, session.Results);

        await component.UpdateAsync(msg =>
        {
            msg.Components = components;
            msg.Embeds = Array.Empty<Embed>();
        });
    }
}
