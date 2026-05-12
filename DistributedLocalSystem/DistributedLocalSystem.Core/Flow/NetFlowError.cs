namespace DistributedLocalSystem.Core.Flow;

/// <summary>Ошибка сценария (код для клиента + сообщение).</summary>
public sealed record NetFlowError(string Code, string Message);
