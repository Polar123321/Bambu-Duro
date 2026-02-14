using Discord;
using System.Text;
using DColor = Discord.Color;
using Microsoft.Extensions.Options;
using ConsoleApp4.Configuration;

namespace ConsoleApp4.Helpers;

public sealed class EmbedHelper
{
    private readonly IOptions<BotConfiguration> _config;
    private readonly Random _random = new();

    public EmbedHelper(IOptions<BotConfiguration> config)
    {
        _config = config;
    }

    public EmbedBuilder CreateDefault(string title, string description)
    {
        return CreateMajestic(title, description, null);
    }

    public EmbedBuilder CreateSuccess(string title, string description)
    {
        return CreateMajestic(title, description, null)
            .WithColor(DColor.Green);
    }

    public EmbedBuilder CreateWarning(string title, string description)
    {
        return CreateMajestic(title, description, null)
            .WithColor(DColor.Orange);
    }

    public EmbedBuilder CreateError(string title, string description)
    {
        return CreateMajestic(title, description, null)
            .WithColor(DColor.Red);
    }

    public EmbedBuilder CreateInfo(string title, string description)
    {
        return CreateMajestic(title, description, null)
            .WithColor(DColor.Blue);
    }

    public EmbedBuilder CreateMajestic(string title, string description, string? imageUrl = null)
    {
        var footer = string.IsNullOrWhiteSpace(_config.Value.ThemeFooter)
            ? "Use !help para ver comandos."
            : _config.Value.ThemeFooter;
        var authorIcon = GetThemeGifUrl();
        var color = ParseColor(_config.Value.EmbedColor);
        var banner = string.IsNullOrWhiteSpace(_config.Value.ThemeBannerUrl)
            ? null
            : _config.Value.ThemeBannerUrl;

        var embed = new EmbedBuilder()
            .WithTitle(title)
            .WithDescription(description)
            .WithColor(color)
            .WithAuthor(_config.Value.ThemeName, authorIcon)
            .WithFooter(footer)
            .WithCurrentTimestamp();

        if (string.IsNullOrWhiteSpace(imageUrl) && !string.IsNullOrWhiteSpace(banner))
        {
            embed.WithImageUrl(banner);
        }

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            embed.WithThumbnailUrl(imageUrl);
        }
        else if (!string.IsNullOrWhiteSpace(authorIcon))
        {
            embed.WithThumbnailUrl(authorIcon);
        }

        return embed;
    }

    public EmbedBuilder CreateMajesticWithImage(string title, string description, string imageUrl)
    {
        var embed = CreateMajestic(title, description, null)
            .WithImageUrl(imageUrl)
            .WithThumbnailUrl(null);

        return embed;
    }

    public MessageComponent BuildDefaultComponents()
    {
        return new ComponentBuilderV2().Build();
    }

    public MessageComponent BuildCv2(EmbedBuilder embed, Action<ComponentBuilderV2>? configure = null)
    {
        var builder = new ComponentBuilderV2();
        AppendEmbedLikeComponents(builder, embed);
        configure?.Invoke(builder);
        return builder.Build();
    }

    public MessageComponent BuildCv2Card(EmbedBuilder embed, Action<ContainerBuilder> configureContainer)
    {
        var builder = new ComponentBuilderV2();
        builder.WithContainer(c =>
        {
            BuildEmbedContainer(c, embed);
            configureContainer(c);
        });
        return builder.Build();
    }

    private static void AppendEmbedLikeComponents(ComponentBuilderV2 builder, EmbedBuilder embed)
    {
        builder.WithContainer(c => BuildEmbedContainer(c, embed));
    }

    private static void BuildEmbedContainer(ContainerBuilder c, EmbedBuilder embed)
    {
        
        

        
        c.WithAccentColor(embed.Color);

        var headerText = BuildHeaderText(embed);
        var accessoryUrl = FirstNonEmpty(embed.ThumbnailUrl, embed.Author?.IconUrl);

        
        if (!string.IsNullOrWhiteSpace(accessoryUrl) && headerText.Length <= TextDisplayBuilder.MaxContentLength)
        {
            var thumb = new ThumbnailBuilder()
                .WithMedia(new UnfurledMediaItemProperties(accessoryUrl));

            c.WithSection(
                new[] { new TextDisplayBuilder(headerText) },
                thumb,
                isSpoiler: false,
                id: null);
        }
        else
        {
            AddTextDisplays(c, headerText);
        }

        if (embed.Fields is { Count: > 0 })
        {
            foreach (var f in embed.Fields)
            {
                var name = f.Name?.Trim();
                var value = f.Value?.ToString()?.Trim();
                if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                c.WithSeparator(SeparatorSpacingSize.Small, true, null);

                var block = string.IsNullOrWhiteSpace(name)
                    ? value!
                    : string.IsNullOrWhiteSpace(value)
                        ? $"### {name}"
                        : $"### {name}\n{value}";

                AddTextDisplays(c, block);
            }
        }

        var footer = BuildFooterText(embed);
        if (!string.IsNullOrWhiteSpace(footer))
        {
            c.WithSeparator(SeparatorSpacingSize.Small, false, null);
            AddTextDisplays(c, footer);
        }

        
        if (!string.IsNullOrWhiteSpace(embed.ImageUrl))
        {
            c.WithMediaGallery(new[] { embed.ImageUrl });
        }
    }

    private static void AddTextDisplays<BuilderT>(BuilderT container, string text)
        where BuilderT : class, IStaticComponentContainer
    {
        foreach (var chunk in Split(text, TextDisplayBuilder.MaxContentLength))
        {
            if (!string.IsNullOrWhiteSpace(chunk))
            {
                container.WithTextDisplay(chunk);
            }
        }
    }

    private static string BuildHeaderText(EmbedBuilder embed)
    {
        var sb = new StringBuilder();

        if (embed.Author is { Name: { Length: > 0 } authorName })
        {
            sb.Append("**").Append(authorName).Append("**\n");
        }

        if (!string.IsNullOrWhiteSpace(embed.Title))
        {
            sb.Append("# ").Append(embed.Title.Trim()).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(embed.Description))
        {
            sb.Append(embed.Description.Trim()).Append('\n');
        }

        var text = sb.ToString().TrimEnd();
        return text.Length == 0 ? "(sem conteudo)" : text;
    }

    private static string? BuildFooterText(EmbedBuilder embed)
    {
        var footerText = embed.Footer?.Text;
        var hasFooter = !string.IsNullOrWhiteSpace(footerText);
        var hasTimestamp = embed.Timestamp.HasValue;
        if (!hasFooter && !hasTimestamp)
        {
            return null;
        }

        var sb = new StringBuilder("-# ");
        if (hasFooter)
        {
            sb.Append(footerText!.Trim());
        }

        if (hasFooter && hasTimestamp)
        {
            sb.Append(" | ");
        }

        if (hasTimestamp)
        {
            sb.Append(embed.Timestamp!.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
        }

        return sb.ToString();
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        foreach (var v in values)
        {
            if (!string.IsNullOrWhiteSpace(v))
            {
                return v.Trim();
            }
        }
        return null;
    }

    private static string ToMarkdown(EmbedBuilder embed)
    {
        var sb = new StringBuilder();

        if (embed.Author is { Name: { Length: > 0 } authorName })
        {
            sb.Append("**").Append(authorName).Append("**\n");
        }

        if (!string.IsNullOrWhiteSpace(embed.Title))
        {
            sb.Append("## ").Append(embed.Title.Trim()).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(embed.Description))
        {
            sb.Append(embed.Description.Trim()).Append('\n');
        }

        if (embed.Fields is { Count: > 0 })
        {
            sb.Append('\n');
            foreach (var f in embed.Fields)
            {
                if (!string.IsNullOrWhiteSpace(f.Name))
                {
                    sb.Append("**").Append(f.Name.Trim()).Append("**\n");
                }

                var value = f.Value?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    sb.Append(value.Trim()).Append('\n');
                }

                sb.Append('\n');
            }
        }

        var footerText = embed.Footer?.Text;
        var hasFooter = !string.IsNullOrWhiteSpace(footerText);
        var hasTimestamp = embed.Timestamp.HasValue;
        if (hasFooter || hasTimestamp)
        {
            sb.Append("-# ");
            if (hasFooter)
            {
                sb.Append(footerText!.Trim());
            }

            if (hasFooter && hasTimestamp)
            {
                sb.Append(" • ");
            }

            if (hasTimestamp)
            {
                sb.Append(embed.Timestamp!.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
            }

            sb.Append('\n');
        }

        var text = sb.ToString().TrimEnd();
        return text.Length == 0 ? "(sem conteudo)" : text;
    }

    private static IEnumerable<string> Split(string text, int maxLen)
    {
        text ??= string.Empty;
        if (text.Length <= maxLen)
        {
            yield return text;
            yield break;
        }

        var idx = 0;
        while (idx < text.Length)
        {
            var remainingLen = text.Length - idx;
            var take = Math.Min(maxLen, remainingLen);

            
            var end = idx + take;
            var cut = text.LastIndexOf('\n', end - 1, take);
            if (cut <= idx)
            {
                cut = text.LastIndexOf(' ', end - 1, take);
            }
            if (cut <= idx)
            {
                cut = end;
            }

            var chunk = text.Substring(idx, cut - idx).TrimEnd();
            if (chunk.Length > 0)
            {
                yield return chunk;
            }

            idx = cut;
            while (idx < text.Length && char.IsWhiteSpace(text[idx]))
            {
                idx++;
            }
        }
    }

    private static Discord.Color ParseColor(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Discord.Color.Gold;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "gold" => Discord.Color.Gold,
            "blue" => Discord.Color.Blue,
            "teal" => Discord.Color.Teal,
            "green" => Discord.Color.Green,
            "red" => Discord.Color.Red,
            "purple" => Discord.Color.Purple,
            "magenta" => Discord.Color.Magenta,
            "orange" => Discord.Color.Orange,
            _ => Discord.Color.Gold
        };
    }

    private string? GetThemeGifUrl()
    {
        var list = _config.Value.ThemeGifUrls;
        if (list == null || list.Count == 0)
        {
            return null;
        }

        return list[_random.Next(list.Count)];
    }
}



