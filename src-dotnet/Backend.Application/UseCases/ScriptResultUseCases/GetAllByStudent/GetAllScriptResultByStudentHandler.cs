using AutoMapper;
using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Abstractions.Repositories;
using Backend.Application.Contracts.ScriptResultContract;
using Backend.Domain.Common;
using Backend.Domain.Models;

namespace Backend.Application.UseCases.ScriptResultUseCases.GetAllByStudent;

public class GetAllScriptResultByStudentHandler
    : QueryHandlerBase<GetAllScriptResultByStudentQuery, List<ScriptResultDto>>
{
    private readonly IScriptResultRepository _scriptResultRepository;

    private readonly IMapper _mapper;

    public GetAllScriptResultByStudentHandler(
        IScriptResultRepository scriptResultRepository,
        IMapper mapper
    )
    {
        _scriptResultRepository = scriptResultRepository;
        _mapper = mapper;
    }

    protected override async Task<Result<List<ScriptResultDto>>> ExecuteAsync(
        GetAllScriptResultByStudentQuery request,
        CancellationToken cancellationToken
    )
    {
        List<ScriptResult> scriptResults = await _scriptResultRepository.GetAllAsync(
            cancellationToken
        );

        return Result<List<ScriptResultDto>>.Success(
            _mapper.Map<List<ScriptResultDto>>(scriptResults)
        );
    }
}
