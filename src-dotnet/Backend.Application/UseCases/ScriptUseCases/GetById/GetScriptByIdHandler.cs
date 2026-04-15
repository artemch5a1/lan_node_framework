using AutoMapper;
using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Abstractions.Repositories;
using Backend.Application.Contracts.ScriptContract;
using Backend.Domain.Common;
using Backend.Domain.Models;

namespace Backend.Application.UseCases.ScriptUseCases.GetById;

public class GetScriptByIdHandler : QueryHandlerBase<GetScriptByIdQuery, ScriptDto>
{
    private readonly IScriptRepository _scriptRepository;

    private readonly IMapper _mapper;

    public GetScriptByIdHandler(IScriptRepository scriptRepository, IMapper mapper)
    {
        _scriptRepository = scriptRepository;
        _mapper = mapper;
    }

    protected override async Task<Result<ScriptDto>> ExecuteAsync(
        GetScriptByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        Script? scripts = await _scriptRepository.GetByIdAsync(request.ScriptId, cancellationToken);

        if (scripts is null)
            return Result<ScriptDto>.FailureNotFound(Script.NameForMessage);

        ScriptDto scriptDtos = _mapper.Map<ScriptDto>(scripts);

        return Result<ScriptDto>.Success(scriptDtos);
    }
}
