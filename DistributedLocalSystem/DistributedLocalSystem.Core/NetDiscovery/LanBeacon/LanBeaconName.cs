using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace DistributedLocalSystem.Core.NetDiscovery.LanBeacon;

/// <summary>
/// Формат v1 имени в UDP beacon (одна строка без расширения библиотеки): <c>DLSv1-&lt;productSlug&gt;-&lt;instanceSlug&gt;</c>.
/// Сегменты — только <c>[a-z0-9]</c>, до 48 символов каждый.
/// Проверки допустимости значений для API — в слое Application.
/// </summary>
public static partial class LanBeaconName
{
    public const string FormatPrefix = "DLSv1";
    public const int MaxSlugLength = 48;

    [GeneratedRegex("^[a-z0-9]{1,48}$", RegexOptions.Compiled)]
    private static partial Regex SlugPattern();

    /// <summary>Синтаксис сегмента DLS (нижний регистр, буквы и цифры, длина).</summary>
    public static bool IsValidSlug(string? s) =>
        !string.IsNullOrEmpty(s) && SlugPattern().IsMatch(s);

    /// <summary>Проверка префикса платформы (до парсинга JSON в будущем).</summary>
    public static bool HasPlatformPrefix(string? fullName) =>
        fullName?.StartsWith($"{FormatPrefix}-", StringComparison.Ordinal) == true;

    public static bool TryParse(string? fullName, out LanBeaconParsed parsed)
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(fullName))
            return false;

        ReadOnlySpan<char> span = fullName.AsSpan();
        ReadOnlySpan<char> expectedPrefix = $"{FormatPrefix}-".AsSpan();
        if (!span.StartsWith(expectedPrefix, StringComparison.Ordinal))
            return false;

        ReadOnlySpan<char> rest = span[expectedPrefix.Length..];
        int lastDash = rest.LastIndexOf('-');
        if (lastDash <= 0 || lastDash >= rest.Length - 1)
            return false;

        string product = rest[..lastDash].ToString();
        string instance = rest[(lastDash + 1)..].ToString();

        if (!IsValidSlug(product) || !IsValidSlug(instance))
            return false;

        parsed = new LanBeaconParsed(product, instance, fullName);
        return true;
    }

    /// <summary>Собирает полное имя beacon без исключений; неудача — пара slug не в синтаксисе DLS.</summary>
    public static bool TryBuild(
        string? productSlug,
        string? instanceSlug,
        [NotNullWhen(true)] out string? fullName
    )
    {
        fullName = null;
        if (!IsValidSlug(productSlug) || !IsValidSlug(instanceSlug))
            return false;

        fullName = $"{FormatPrefix}-{productSlug}-{instanceSlug}";
        return true;
    }

    /// <summary>Полное имя beacon для синтаксически допустимой пары slug’ов; иначе пустая строка.</summary>
    public static string FormatFullNameOrEmpty(string? productSlug, string? instanceSlug) =>
        TryBuild(productSlug, instanceSlug, out string? full) ? full : string.Empty;

    /// <summary>То же для отображения узла; при недопустимой паре — «—».</summary>
    public static string FormatFullNameOrDash(string? productSlug, string? instanceSlug) =>
        TryBuild(productSlug, instanceSlug, out string? full) ? full : "—";

    /// <summary>
    /// Приводит произвольную строку к slug: нижний регистр, только буквы/цифры, обрезка по длине.
    /// Пустой результат заменяется на <paramref name="fallback"/>.
    /// </summary>
    public static string SlugifyLegacy(string? raw, string fallback = "app")
    {
        if (string.IsNullOrWhiteSpace(raw))
            return fallback;

        Span<char> buffer = stackalloc char[MaxSlugLength];
        int n = 0;
        foreach (char c in raw.ToLowerInvariant())
        {
            if (n >= MaxSlugLength)
                break;
            if (c is >= 'a' and <= 'z' or >= '0' and <= '9')
                buffer[n++] = c;
        }

        if (n == 0)
            return fallback;

        return new string(buffer[..n]);
    }
}

public readonly record struct LanBeaconParsed(
    string ProductSlug,
    string InstanceSlug,
    string FullName
);
