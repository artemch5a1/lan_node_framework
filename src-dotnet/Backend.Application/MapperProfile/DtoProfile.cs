using AutoMapper;
using Backend.Application.Contracts.ExecutorContract;
using Backend.Application.Contracts.ScriptContract;
using Backend.Application.Contracts.ScriptResultContract;
using Backend.Domain.Models;

namespace Backend.Application.MapperProfile;

public class DtoProfile : Profile
{
    public DtoProfile()
    {
        CreateMap<Script, ScriptDto>();

        CreateMap<Executor, ExecutorDto>();

        CreateMap<ScriptResult, ScriptResultDto>();

        CreateMap<ScriptResult, ScriptResultCreatedDto>();
    }
}
