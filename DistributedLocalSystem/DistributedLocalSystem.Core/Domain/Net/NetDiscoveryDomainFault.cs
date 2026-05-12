using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Core.Domain.Net;

/// <summary>Доменная причина сбоя (без деталей UDP/транспорта).</summary>
public abstract record NetDiscoveryDomainFault(string Code, string Message)
{
    public NetFlowError ToFlowError() => new(Code, Message);
}

/// <summary>В LAN уже есть другой хост с тем же идентификатором приложения.</summary>
public sealed record AnotherHostAlreadyPresentFault(string Detail)
    : NetDiscoveryDomainFault(
        NetFlowErrorCodes.AnotherHostAlreadyPresent,
        string.IsNullOrWhiteSpace(Detail)
            ? "В сети уже обнаружен другой хост для этого приложения."
            : Detail
    );
