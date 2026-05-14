using DistributedLocalSystem.Core.Flow;

namespace DistributedLocalSystem.Core.Domain.Net;

/// <summary>Доменная причина сбоя (без деталей UDP/транспорта).</summary>
public abstract record NetDiscoveryDomainFault(string Code, string Message)
{
    public NetFlowError ToFlowError() => new(Code, Message);
}

/// <summary>В LAN уже есть другой хост с тем же идентификатором приложения.</summary>
public sealed record AnotherHostAlreadyPresentFault()
    : NetDiscoveryDomainFault(
        NetFlowErrorCodes.AnotherHostAlreadyPresent,
        "В этой сети уже запущен другой узел в режиме хоста с тем же приложением. "
            + "Закройте лишний экземпляр или измените product / instance slug."
    );
