using System.Net;

namespace DistributedLocalSystem.Core.Abstractions;

/// <summary>Проверка, относится ли IP-адрес к этому компьютеру.</summary>
public interface ILocalMachineAddressMatcher
{
    bool IsLocalMachine(IPAddress target, string? reportedPrimaryHostIp);
}
