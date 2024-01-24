using Infrastructure.Database;
using MediatR;
using Shared.ApiModels;

namespace Application.SystemLogFolder.CQRS;

public record GetSystemLogsSinceQuery(long LongsSinceUnixMilliseconds) : IRequest<List<SystemLogResponse>>;

public class GetSystemLogsSinceQueryHandler : IRequestHandler<GetSystemLogsSinceQuery, List<SystemLogResponse>>
{
    private readonly UnitOfWork _unitOfWork;

    public GetSystemLogsSinceQueryHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<List<SystemLogResponse>> Handle(GetSystemLogsSinceQuery request, CancellationToken cancellationToken)
    {
        var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(request.LongsSinceUnixMilliseconds).DateTime;
        var logs = await _unitOfWork.SystemLogRepo.GetLogsSince(logsSinceDateTime);
        var response = logs.Select(
                log => new SystemLogResponse(
                    log.Message,
                    log.CreatedUtc)
            )
            .ToList();

        return response;
    }
}