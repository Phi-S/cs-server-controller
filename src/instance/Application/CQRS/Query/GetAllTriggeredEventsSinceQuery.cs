using Infrastructure.Database;
using MediatR;
using Shared.ApiModels;

namespace Application.CQRS.Query;

public record GetAllTriggeredEventsSinceQuery(long LongsSinceUnixMilliseconds) : IRequest<List<EventLogResponse>>;

public class
    GetAllTriggeredEventsSinceQueryHandler : IRequestHandler<GetAllTriggeredEventsSinceQuery, List<EventLogResponse>>
{
    private readonly UnitOfWork _unitOfWork;

    public GetAllTriggeredEventsSinceQueryHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<List<EventLogResponse>> Handle(GetAllTriggeredEventsSinceQuery request, CancellationToken cancellationToken)
    {
        var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(request.LongsSinceUnixMilliseconds).DateTime;
        var logs = await _unitOfWork.EventLogRepo.GetLogsSince(logsSinceDateTime);
        var response = logs.Select(
                log => new EventLogResponse(
                    log.Name,
                    log.TriggeredAtUtc)
            )
            .ToList();

        return response;
    }
}