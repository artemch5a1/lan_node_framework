using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Contracts.ScriptResultContract;
using Backend.Domain.Models;

namespace Backend.Application.UseCases.ScriptResultUseCases.GetAll;

public record GetAllScriptResultQuery() : IQuery<List<ScriptResultDto>>;
