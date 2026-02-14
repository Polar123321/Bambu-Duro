namespace ConsoleApp4.Services.Models;

public sealed class GuildConfig
{
    public bool NsfwEnabled { get; set; }
    public bool RequireNsfwChannel { get; set; } = true;
    public string ThemeName { get; set; } = "Shaco";
    public string ThemeFooter { get; set; } = "Shaco, o Bufao Demoniaco";
    public string ThemeColor { get; set; } = "Red";
    public string ThemeStyle { get; set; } = "Majestic";
    public string ImageProvider { get; set; } = "waifu.pics";
    public ulong WelcomeChannelId { get; set; }
    public ulong ReceptionistRoleId { get; set; }
    public string WelcomeTitle { get; set; } = "Bem-vindo(a)!";
    public string WelcomeDescription { get; set; } =
        "Bem-vindo(a) {user} ao **{guild}**!\nAgora somos **{memberCount}** membros.";
    public string WelcomeImageUrl { get; set; } = string.Empty;
    public bool WelcomeUseWaifuImage { get; set; } = true;
    public string WelcomeButtonLabel { get; set; } = "Dar boas-vindas";
    public string WelcomeColor { get; set; } = "Gold";
    public bool StaffDmEnabled { get; set; }
    public string StaffDmApproved { get; set; } =
        "Ola {user}, sua candidatura no servidor **{guild}** foi **APROVADA**. Parabens!";
    public string StaffDmDenied { get; set; } =
        "Ola {user}, sua candidatura no servidor **{guild}** foi **NEGADA**. Obrigado pelo interesse.";
}
