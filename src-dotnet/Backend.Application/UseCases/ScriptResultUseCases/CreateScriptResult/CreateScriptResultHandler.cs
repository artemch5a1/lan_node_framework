using AutoMapper;
using Backend.Application.Abstractions.Mediatr;
using Backend.Application.Abstractions.Repositories;
using Backend.Application.Abstractions.Repositories.Base;
using Backend.Application.Contracts.ExecutorContract;
using Backend.Application.Contracts.ScriptResultContract;
using Backend.Domain.Common;
using Backend.Domain.Models;

namespace Backend.Application.UseCases.ScriptResultUseCases.CreateScriptResult;

public class CreateScriptResultHandler
    : CommandHandlerBase<CreateScriptResultCommand, ScriptResultCreatedDto>
{
    private readonly IScriptResultRepository _scriptResultRepository;
    private readonly IExecutorRepository _executorRespository;
    private readonly IMapper _mapper;

    public CreateScriptResultHandler(
        IUnitOfWork unitOfWork,
        IScriptResultRepository scriptResultRepository,
        IExecutorRepository executorRepository,
        IMapper mapper
    )
        : base(unitOfWork)
    {
        _scriptResultRepository = scriptResultRepository;
        _executorRespository = executorRepository;
        _mapper = mapper;
    }

    protected override async Task<Result<ScriptResultCreatedDto>> ExecuteAsync(
        CreateScriptResultCommand request,
        CancellationToken cancellationToken
    )
    {
        ExecutorCreateDto executorCreateDto = request.ScriptResultCreate.Executor;

        ScriptResultCreateDto scriptResultCreateDto = request.ScriptResultCreate;

        Executor executor = Executor.Create(
            executorCreateDto.Surname,
            executorCreateDto.Name,
            executorCreateDto.Patronymic
        );

        Executor createdExecutor = await _executorRespository.AddAsync(executor, cancellationToken);

        ScriptResult scriptResult = ScriptResult.Create(
            createdExecutor.Id,
            scriptResultCreateDto.ScriptId,
            scriptResultCreateDto.StartedAt,
            scriptResultCreateDto.SessionName,
            scriptResultCreateDto.PointScored,
            scriptResultCreateDto.TotalPoint
        );

        ScriptResult createdScriptResult = await _scriptResultRepository.AddAsync(
            scriptResult,
            cancellationToken
        );

        return Result<ScriptResultCreatedDto>.Success(
            _mapper.Map<ScriptResultCreatedDto>(createdScriptResult)
        );
    }
}
