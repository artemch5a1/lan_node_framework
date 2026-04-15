using AutoMapper;
using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Abstractions.Repositories;
using Backend.Application.Contracts.ScriptContract;
using Backend.Domain.Common;
using Backend.Domain.Models;

namespace Backend.Application.UseCases.ScriptUseCases.GetAll;

public class GetAllScriptHandler : QueryHandlerBase<GetAllScriptQuery, List<ScriptDto>>
{
    private readonly IScriptRepository _scriptRepository;

    private readonly IMapper _mapper;

    public GetAllScriptHandler(IScriptRepository scriptRepository, IMapper mapper)
    {
        _scriptRepository = scriptRepository;
        _mapper = mapper;
    }

    protected override async Task<Result<List<ScriptDto>>> ExecuteAsync(
        GetAllScriptQuery request,
        CancellationToken cancellationToken
    )
    {
        List<Script> scripts = await _scriptRepository.GetAllAsync(cancellationToken);

        List<ScriptDto> scriptDtos = _mapper.Map<List<ScriptDto>>(scripts);

        return Result<List<ScriptDto>>.Success(scriptDtos);
    }
}
