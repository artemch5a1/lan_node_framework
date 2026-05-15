namespace DistributedLocalSystem.Core.NetDiscovery.Model;

/// <summary>Локальный IPv4 и подпись адаптера (как в списке сетевых интерфейсов ОС).</summary>
public sealed record NetLocalIpv4Endpoint(string Address, string InterfaceDescription);
