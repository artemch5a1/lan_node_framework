using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Infrastructure.Persistence.Entities;

namespace DistributedLocalSystem.Infrastructure.Persistence;

internal static class NetDiscoveryRowNormalizer
{
    internal static string NewRandomInstanceSlug()
    {
        const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
        char[] buffer = new char[8];
        for (int i = 0; i < buffer.Length; i++)
            buffer[i] = chars[Random.Shared.Next(chars.Length)];
        return new string(buffer);
    }

    /// <param name="legacyAppId">
    /// Значение колонки AppId в старых БД (до вычисляемого beacon-имени); для новых таблиц без колонки — null.
    /// </param>
    internal static void Normalize(NetDiscoverySettingsEntity e, string? legacyAppId)
    {
        if (
            string.Equals(e.Role, "none", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(e.RemoteHostIp)
        )
            e.Role = "host";

        if (
            string.Equals(e.Role, "client", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(e.RemoteHostIp)
        )
            e.Role = "host";

        if (string.IsNullOrEmpty(e.InstanceGuid))
            e.InstanceGuid = Guid.NewGuid().ToString("N");

        if (string.IsNullOrEmpty(e.ProductSlug) || string.IsNullOrEmpty(e.InstanceSlug))
        {
            if (!string.IsNullOrEmpty(legacyAppId) && LanBeaconName.TryParse(legacyAppId, out LanBeaconParsed parsed))
            {
                if (string.IsNullOrEmpty(e.ProductSlug))
                    e.ProductSlug = parsed.ProductSlug;
                if (string.IsNullOrEmpty(e.InstanceSlug))
                    e.InstanceSlug = parsed.InstanceSlug;
            }
            else if (!string.IsNullOrEmpty(legacyAppId))
            {
                if (string.IsNullOrEmpty(e.ProductSlug))
                    e.ProductSlug = LanBeaconName.SlugifyLegacy(legacyAppId);
                if (string.IsNullOrEmpty(e.InstanceSlug))
                    e.InstanceSlug = NewRandomInstanceSlug();
            }
        }
    }
}
