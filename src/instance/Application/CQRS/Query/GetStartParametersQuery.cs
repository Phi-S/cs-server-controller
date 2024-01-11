using Application.StartParameterFolder;
using ErrorOr;
using MediatR;
using Shared.ApiModels;

namespace Application.CQRS.Query;

public record GetStartParametersQuery() : IRequest<ErrorOr<StartParameters>>;

public class GetStartParametersQueryHandler : IRequestHandler<GetStartParametersQuery, ErrorOr<StartParameters>>
{
    private readonly StartParameterService _startParameterService;

    public GetStartParametersQueryHandler(StartParameterService startParameterService)
    {
        _startParameterService = startParameterService;
    }
    
    public Task<ErrorOr<StartParameters>> Handle(GetStartParametersQuery request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_startParameterService.Get());
    }
}