using AutoMapper;
using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Abstractions.Repositories;
using Backend.Application.Contracts.ScriptResultContract;
using Backend.Domain.Common;
using Backend.Domain.Models;

namespace Backend.Application.UseCases.ScriptResultUseCases.GetById;

public class GetByIdScriptResultHandler
    : QueryHandlerBase<GetByIdScriptResultQuery, ScriptResultDto>
{
    private readonly IScriptResultRepository _scriptResultRepository;

    private readonly IMapper _mapper;

    public GetByIdScriptResultHandler(
        IScriptResultRepository scriptResultRepository,
        IMapper mapper
    )
    {
        _scriptResultRepository = scriptResultRepository;
        _mapper = mapper;
    }

    protected override async Task<Result<ScriptResultDto>> ExecuteAsync(
        GetByIdScriptResultQuery request,
        CancellationToken cancellationToken
    )
    {
        ScriptResult? scriptResults = await _scriptResultRepository.GetByIdAsync(
            request.ScriptResultId,
            cancellationToken
        );

        if (scriptResults is null)
            return Result<ScriptResultDto>.FailureNotFound(ScriptResult.NameForMessage);

        return Result<ScriptResultDto>.Success(_mapper.Map<ScriptResultDto>(scriptResults));
    }
}
