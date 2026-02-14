using System.ComponentModel.DataAnnotations;

namespace ConsoleApp4.Configuration;

public sealed class BotConfiguration
{
    [Required]
    public string Token { get; init; } = string.Empty;

    [Required]
    public string Prefix { get; init; } = "!";

    public ulong OwnerUserId { get; init; }

    public string Status { get; init; } = "online";

    public string EmbedColor { get; init; } = "Gold";

    public string ThemeName { get; init; } = "Shaco";

    public string ThemeFooter { get; init; } = "Shaco, o Bufao Demoniaco";

    public List<string> ThemeGifUrls { get; init; } = new();

    public string ThemeBannerUrl { get; init; } = "https://ddragon.leagueoflegends.com/cdn/img/champion/splash/Shaco_0.jpg";

    public string ThemeActionLabel { get; init; } = "Ver opcoes";

    public string ThemeActionEmoji { get; init; } = "💲";
}
