using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Contracts.ScriptResultContract;
using Backend.Domain.Common;

namespace Backend.Application.UseCases.ScriptResultUseCases.CreateScriptResult;

public record CreateScriptResultCommand(ScriptResultCreateDto ScriptResultCreate)
    : ICommand<ScriptResultCreatedDto>;
