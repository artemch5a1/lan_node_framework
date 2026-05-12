using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.Persistence.Entities;

namespace DistributedLocalSystem.Core.Persistence;

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

    internal static void Normalize(NetDiscoverySettingsEntity e)
    {
        if (string.IsNullOrEmpty(e.InstanceGuid))
            e.InstanceGuid = Guid.NewGuid().ToString("N");

        if (string.IsNullOrEmpty(e.ProductSlug) || string.IsNullOrEmpty(e.InstanceSlug))
        {
            if (LanBeaconName.TryParse(e.AppId, out LanBeaconParsed parsed))
            {
                if (string.IsNullOrEmpty(e.ProductSlug))
                    e.ProductSlug = parsed.ProductSlug;
                if (string.IsNullOrEmpty(e.InstanceSlug))
                    e.InstanceSlug = parsed.InstanceSlug;
            }
            else
            {
                if (string.IsNullOrEmpty(e.ProductSlug))
                    e.ProductSlug = LanBeaconName.SlugifyLegacy(e.AppId);
                if (string.IsNullOrEmpty(e.InstanceSlug))
                    e.InstanceSlug = NewRandomInstanceSlug();
            }
        }

        if (LanBeaconName.IsValidSlug(e.ProductSlug) && LanBeaconName.IsValidSlug(e.InstanceSlug))
        {
            string rebuilt = LanBeaconName.Build(e.ProductSlug, e.InstanceSlug);
            if (!string.Equals(e.AppId, rebuilt, StringComparison.Ordinal))
                e.AppId = rebuilt;
        }
    }
}
