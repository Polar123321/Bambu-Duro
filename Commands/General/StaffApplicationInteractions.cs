using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ConsoleApp4.Helpers;
using ConsoleApp4.Services.Interfaces;
using ConsoleApp4.Services.Models;

namespace ConsoleApp4.Commands.General;

public sealed class StaffApplicationInteractions : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IStaffApplicationStore _store;
    private readonly IGuildConfigStore _configStore;
    private readonly EmbedHelper _embeds;

    public StaffApplicationInteractions(IStaffApplicationStore store, IGuildConfigStore configStore, EmbedHelper embeds)
    {
        _store = store;
        _configStore = configStore;
        _embeds = embeds;
    }

    [ComponentInteraction("staff:open")]
    public async Task OpenFormAsync()
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Este formulario so pode ser usado em servidores.", ephemeral: true);
            return;
        }

        await RespondAsync("Use `*staff` e escolha um cargo para abrir o formulario.", ephemeral: true);
    }

    [ComponentInteraction("staff:role:select")]
    public async Task RoleSelectAsync(string[] selections)
    {
        if (selections.Length == 0)
        {
            await RespondAsync("Selecione um cargo.", ephemeral: true);
            return;
        }

        var roleId = selections[0];
        await OpenRoleFormAsync(roleId);
    }

    [ComponentInteraction("staff:form:open:role:*")]
    public async Task OpenRoleFormButtonAsync(string roleIdRaw)
    {
        await OpenRoleFormAsync(roleIdRaw);
    }

    [ModalInteraction("staff:submit:role:*")]
    public async Task SubmitWithRoleAsync(string roleIdRaw)
    {
        await HandleSubmitAsync(roleIdRaw);
    }

    private async Task OpenRoleFormAsync(string roleIdRaw)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Este formulario so pode ser usado em servidores.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(roleIdRaw, out var parsedRoleId))
        {
            await RespondAsync("Cargo invalido.", ephemeral: true);
            return;
        }

        var role = Context.Guild.GetRole(parsedRoleId);
        if (role == null)
        {
            await RespondAsync("Cargo nao encontrado.", ephemeral: true);
            return;
        }

        var extraQuestions = await _store.GetQuestionsForRoleAsync(Context.Guild.Id, role.Id);
        var modal = BuildApplicationModal(role.Id, role.Name, extraQuestions);
        await Context.Interaction.RespondWithModalAsync(modal);
    }

    private async Task HandleSubmitAsync(string? roleIdRaw)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Este formulario so pode ser usado em servidores.", ephemeral: true);
            return;
        }

        if (await _store.IsBannedAsync(Context.Guild.Id, Context.User.Id))
        {
            await RespondAsync("Voce nao pode mais se candidatar neste servidor.", ephemeral: true);
            return;
        }

        var modalData = Context.Interaction as SocketModal;
        if (modalData == null)
        {
            await RespondAsync("Formulario invalido.", ephemeral: true);
            return;
        }

        ulong? roleId = null;
        string? roleName = null;
        if (!string.IsNullOrWhiteSpace(roleIdRaw) && ulong.TryParse(roleIdRaw, out var parsedRoleId))
        {
            var role = Context.Guild.GetRole(parsedRoleId);
            if (role != null)
            {
                roleId = role.Id;
                roleName = role.Name;
            }
        }

        var values = modalData.Data.Components.ToDictionary(c => c.CustomId, c => c.Value);
        if (!values.TryGetValue("motivation", out var motivation) ||
            !values.TryGetValue("experience", out var experience) ||
            !values.TryGetValue("availability", out var availability))
        {
            await RespondAsync("Formulario incompleto.", ephemeral: true);
            return;
        }

        var extraQuestions = (await _store.GetQuestionsForRoleAsync(Context.Guild.Id, roleId)).ToList();
        values.TryGetValue("extra_answers", out var extraAnswers);

        var applicationId = Guid.NewGuid();
        var application = new StaffApplication(
            applicationId,
            Context.Guild.Id,
            Context.User.Id,
            Context.User.Username,
            DateTime.UtcNow,
            motivation,
            experience,
            availability,
            "Pendente",
            null,
            null,
            roleId,
            roleName,
            extraQuestions,
            string.IsNullOrWhiteSpace(extraAnswers) ? null : extraAnswers.Trim());

        await _store.AddAsync(application);

        var embed = _embeds.CreateSuccess("Candidatura enviada",
                "Sua candidatura foi registrada e sera analisada pela equipe.");

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);

        await TrySendToStaffChannelAsync(application);
    }

    [ComponentInteraction("staff:admin:open")]
    public async Task OpenAdminFormAsync()
    {
        await RespondWithModalAsync<StaffReviewModal>("staff:admin:review");
    }

    [ModalInteraction("staff:admin:review")]
    public async Task ReviewAsync(StaffReviewModal modal)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Este formulario so pode ser usado em servidores.", ephemeral: true);
            return;
        }

        if (Context.User is SocketGuildUser guildUser &&
            !guildUser.GuildPermissions.ManageGuild)
        {
            await RespondAsync("Permissao insuficiente.", ephemeral: true);
            return;
        }

        if (!ulong.TryParse(modal.UserId, out var userId))
        {
            await RespondAsync("Informe o ID do usuario.", ephemeral: true);
            return;
        }

        var list = await _store.GetByUserAsync(Context.Guild.Id, userId);
        if (list.Count == 0)
        {
            await RespondAsync("Nenhuma candidatura encontrada.", ephemeral: true);
            return;
        }

        var latest = list.First();
        var embed = _embeds.CreateInfo("Candidatura encontrada",
                $"Usuario: **{latest.Username}** ({latest.UserId})")
            .AddField("Status", latest.Status, false)
            .AddField("Cargo", latest.RoleName ?? "Nao informado", false)
            .AddField("Motivacao", latest.Motivation, false)
            .AddField("Experiencia", latest.Experience, false)
            .AddField("Disponibilidade", latest.Availability, false)
            .AddField("Enviado em", latest.SubmittedAtUtc.ToString("dd/MM/yyyy HH:mm"), false);

        AppendExtraQuestions(embed, latest);

        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    [ComponentInteraction("staff:decide:deny:*")]
    public async Task DenyAsync(string applicationIdRaw)
    {
        await DecideAsync(applicationIdRaw, "Negado", applyBan: false, applyMute: false);
    }

    [ComponentInteraction("staff:decide:mute:*")]
    public async Task MuteAsync(string applicationIdRaw)
    {
        await DecideAsync(applicationIdRaw, "Mutado", applyBan: false, applyMute: true);
    }

    [ComponentInteraction("staff:decide:ban:*")]
    public async Task BanAsync(string applicationIdRaw)
    {
        await DecideAsync(applicationIdRaw, "Banido de candidaturas", applyBan: true, applyMute: false);
    }

    [ComponentInteraction("staff:decide:approve:*")]
    public async Task ApproveAsync(string applicationIdRaw)
    {
        await DecideAsync(applicationIdRaw, "Aprovado", applyBan: false, applyMute: false);
    }

    private async Task DecideAsync(string applicationIdRaw, string status, bool applyBan, bool applyMute)
    {
        if (Context.Guild == null)
        {
            await RespondAsync("Guild indisponivel.", ephemeral: true);
            return;
        }

        if (Context.User is SocketGuildUser guildUser &&
            !guildUser.GuildPermissions.ManageGuild)
        {
            await RespondAsync("Permissao insuficiente.", ephemeral: true);
            return;
        }

        if (!Guid.TryParse(applicationIdRaw, out var applicationId))
        {
            await RespondAsync("Candidatura invalida.", ephemeral: true);
            return;
        }

        var application = await _store.GetByIdAsync(Context.Guild.Id, applicationId);
        if (application == null)
        {
            await RespondAsync("Candidatura nao encontrada.", ephemeral: true);
            return;
        }

        await _store.UpdateStatusAsync(Context.Guild.Id, applicationId, status);

        if (applyBan)
        {
            await _store.BanUserAsync(Context.Guild.Id, application.UserId);
        }

        if (applyMute)
        {
            var member = Context.Guild.GetUser(application.UserId);
            if (member != null)
            {
                await member.SetTimeOutAsync(TimeSpan.FromMinutes(10), new RequestOptions { AuditLogReason = "Staff application" });
            }
        }

        await UpdateStaffMessageAsync(application, status);
        await TrySendDecisionDmAsync(application, status);

        var embed = _embeds.CreateSuccess("Status atualizado", $"Novo status: **{status}**");
        await RespondAsync(components: _embeds.BuildCv2(embed), ephemeral: true);
    }

    private async Task UpdateStaffMessageAsync(StaffApplication application, string status)
    {
        if (Context.Guild == null || application.MessageId == null)
        {
            return;
        }

        var channelId = await _store.GetChannelAsync(Context.Guild.Id);
        if (channelId == 0)
        {
            return;
        }

        if (Context.Guild.GetTextChannel(channelId) is not IMessageChannel channel)
        {
            return;
        }

        if (await channel.GetMessageAsync(application.MessageId.Value) is IUserMessage message)
        {
            var components = BuildStaffMessageComponents(application, status, Context.Client.CurrentUser.GetAvatarUrl());
            await message.ModifyAsync(msg =>
            {
                msg.Components = components;
                msg.Embeds = Array.Empty<Embed>();
            });
        }
    }

    private async Task TrySendDecisionDmAsync(StaffApplication application, string status)
    {
        if (Context.Guild == null)
        {
            return;
        }

        var config = await _configStore.GetAsync(Context.Guild.Id);
        if (!config.StaffDmEnabled)
        {
            return;
        }

        var template = IsApprovedStatus(status) ? config.StaffDmApproved : config.StaffDmDenied;
        if (string.IsNullOrWhiteSpace(template))
        {
            return;
        }

        var user = Context.Guild.GetUser(application.UserId) ?? Context.Client.GetUser(application.UserId);
        if (user == null)
        {
            return;
        }

        var message = ExpandStaffTemplate(template, user, Context.Guild, status, application.RoleName);
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            await user.SendMessageAsync(message);
        }
        catch
        {
            // ignore DM failures
        }
    }

    private static bool IsApprovedStatus(string status)
    {
        return status.Equals("Aprovado", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExpandStaffTemplate(string template, IUser user, SocketGuild guild, string status, string? roleName)
    {
        return template
            .Replace("{user}", user.Mention, StringComparison.OrdinalIgnoreCase)
            .Replace("{userName}", user.Username, StringComparison.OrdinalIgnoreCase)
            .Replace("{guild}", guild.Name, StringComparison.OrdinalIgnoreCase)
            .Replace("{status}", status, StringComparison.OrdinalIgnoreCase)
            .Replace("{role}", roleName ?? "Nao informado", StringComparison.OrdinalIgnoreCase);
    }

    private async Task TrySendToStaffChannelAsync(StaffApplication application)
    {
        if (Context.Guild == null)
        {
            return;
        }

        var channelId = await _store.GetChannelAsync(Context.Guild.Id);
        if (channelId == 0)
        {
            return;
        }

        if (Context.Guild.GetTextChannel(channelId) is not IMessageChannel channel)
        {
            return;
        }

        var components = BuildStaffMessageComponents(application, application.Status,
            Context.User.GetAvatarUrl() ?? Context.User.GetDefaultAvatarUrl());

        var message = await channel.SendMessageAsync(components: components);
        await _store.SetMessageIdAsync(application.GuildId, application.ApplicationId, message.Id);
    }

    private EmbedBuilder BuildStaffEmbed(StaffApplication application, string status, string? avatarUrl)
    {
        var embed = _embeds.CreateInfo("Nova candidatura a staff", $"Status: **{status}**")
            .AddField("Usuario", $"{application.Username} ({application.UserId})", false)
            .AddField("Cargo", application.RoleName ?? "Nao informado", false)
            .AddField("Motivacao", application.Motivation, false)
            .AddField("Experiencia", application.Experience, false)
            .AddField("Disponibilidade", application.Availability, false)
            .AddField("Enviado em", application.SubmittedAtUtc.ToString("dd/MM/yyyy HH:mm"), false)
            .WithThumbnailUrl(avatarUrl);

        AppendExtraQuestions(embed, application);
        return embed;
    }

    private MessageComponent BuildStaffMessageComponents(StaffApplication application, string status, string? avatarUrl)
    {
        var embed = BuildStaffEmbed(application, status, avatarUrl);
        return _embeds.BuildCv2Card(embed, c =>
        {
            c.WithActionRow(new[]
            {
                new ButtonBuilder()
                    .WithLabel("Aprovar")
                    .WithCustomId($"staff:decide:approve:{application.ApplicationId}")
                    .WithStyle(ButtonStyle.Success),
                new ButtonBuilder()
                    .WithLabel("Negar")
                    .WithCustomId($"staff:decide:deny:{application.ApplicationId}")
                    .WithStyle(ButtonStyle.Danger),
                new ButtonBuilder()
                    .WithLabel("Mutar")
                    .WithCustomId($"staff:decide:mute:{application.ApplicationId}")
                    .WithStyle(ButtonStyle.Secondary),
                new ButtonBuilder()
                    .WithLabel("Banir")
                    .WithCustomId($"staff:decide:ban:{application.ApplicationId}")
                    .WithStyle(ButtonStyle.Danger)
            });
        });
    }

    private Modal BuildApplicationModal(ulong roleId, string roleName, IReadOnlyList<string> extraQuestions)
    {
        var builder = new ModalBuilder()
            .WithTitle($"Candidatura - {Shorten(roleName, 32)}")
            .WithCustomId($"staff:submit:role:{roleId}")
            .AddTextInput(
                "Por que voce quer ser staff?",
                "motivation",
                TextInputStyle.Paragraph,
                placeholder: "Explique sua motivacao de forma objetiva",
                maxLength: 300)
            .AddTextInput(
                "Experiencia com moderacao",
                "experience",
                TextInputStyle.Paragraph,
                placeholder: "Fale da sua experiencia real",
                maxLength: 300)
            .AddTextInput(
                "Disponibilidade (dias/horarios)",
                "availability",
                TextInputStyle.Short,
                placeholder: "Ex: Seg-Sex, 19-22",
                maxLength: 100);

        if (extraQuestions.Count > 0)
        {
            var prompt = string.Join(" | ", extraQuestions.Select((q, i) => $"{i + 1}) {q}"));
            builder.AddTextInput(
                "Perguntas extras (responda numerado)",
                "extra_answers",
                TextInputStyle.Paragraph,
                placeholder: Shorten(prompt, 100),
                maxLength: 1000,
                required: false);
        }

        return builder.Build();
    }

    private static void AppendExtraQuestions(EmbedBuilder embed, StaffApplication application)
    {
        if (application.ExtraQuestions == null || application.ExtraQuestions.Count == 0)
        {
            return;
        }

        var lines = application.ExtraQuestions
            .Select((q, i) => $"{i + 1}. {q}")
            .ToArray();

        embed.AddField("Perguntas extras", string.Join("\n", lines), false);
        embed.AddField("Respostas extras", application.ExtraAnswers ?? "Nao informado", false);
    }

    private static string Shorten(string value, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= maxLen)
        {
            return value;
        }

        return value[..(maxLen - 3)] + "...";
    }

    public class StaffReviewModal : IModal
    {
        public string Title => "Consultar candidatura";

        [InputLabel("ID do usuario")]
        [ModalTextInput("userid", TextInputStyle.Short, maxLength: 20, placeholder: "Ex: 123456789")]
        public string UserId { get; set; } = string.Empty;
    }
}
