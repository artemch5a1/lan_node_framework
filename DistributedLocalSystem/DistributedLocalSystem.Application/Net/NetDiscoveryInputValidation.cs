using System.Diagnostics.CodeAnalysis;
using DistributedLocalSystem.Core.Flow;
using DistributedLocalSystem.Core.NetDiscovery.LanBeacon;
using DistributedLocalSystem.Core.NetDiscovery.Model;

namespace DistributedLocalSystem.Application.Net;

/// <summary>Валидация входных данных LAN только на уровне Application.</summary>
public static class NetDiscoveryInputValidation
{
    public static bool TryValidatePersist(
        DiscoveryOptions o,
        [NotNullWhen(false)] out NetFlowError? error
    )
    {
        if (!LanBeaconName.IsValidSlug(o.ProductSlug?.Trim()))
        {
            error = new NetFlowError(
                NetFlowErrorCodes.InvalidConfiguration,
                "ProductSlug должен быть непустым slug [a-z0-9], до 48 символов."
            );
            return false;
        }

        if (!LanBeaconName.IsValidSlug(o.InstanceSlug?.Trim()))
        {
            error = new NetFlowError(
                NetFlowErrorCodes.InvalidConfiguration,
                "InstanceSlug должен быть непустым slug [a-z0-9], до 48 символов."
            );
            return false;
        }

        if (o.UdpPort <= 0 || o.UdpPort > 65535 || o.LanPort <= 0 || o.LanPort > 65535)
        {
            error = new NetFlowError(
                NetFlowErrorCodes.InvalidConfiguration,
                "UdpPort и LanPort должны быть в диапазоне 1–65535."
            );
            return false;
        }

        if (o.BeaconIntervalMs <= 0 || o.DiscoveryTimeoutMs <= 0 || o.ProtocolVersion < 1)
        {
            error = new NetFlowError(
                NetFlowErrorCodes.InvalidConfiguration,
                "Интервалы и версия протокола должны быть положительными."
            );
            return false;
        }

        error = null;
        return true;
    }

    public static bool TryValidateLanPeerScanProduct(
        string productSlugTrimmed,
        [NotNullWhen(false)] out NetFlowError? error
    )
    {
        if (!LanBeaconName.IsValidSlug(productSlugTrimmed))
        {
            error = new NetFlowError(
                NetFlowErrorCodes.InvalidConfiguration,
                "ProductSlug не соответствует формату DLS slug; сканирование LAN недоступно."
            );
            return false;
        }

        error = null;
        return true;
    }
}
