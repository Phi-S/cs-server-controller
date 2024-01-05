using Infrastructure.Database;
using MediatR;
using Shared.ApiModels;

namespace Application.CQRS.Query;

public record GetAllTriggersForEventSinceQuery(string EventName, long LongsSinceUnixMilliseconds)
    : IRequest<List<EventLogResponse>>;

public class
    GetAllTriggersForEventSinceQueryHandler : IRequestHandler<GetAllTriggersForEventSinceQuery, List<EventLogResponse>>
{
    private readonly UnitOfWork _unitOfWork;

    public GetAllTriggersForEventSinceQueryHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<EventLogResponse>> Handle(GetAllTriggersForEventSinceQuery request,
        CancellationToken cancellationToken)
    {
        var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(request.LongsSinceUnixMilliseconds).DateTime;
        var logs = await _unitOfWork.EventLogRepo.GetLogsSince(logsSinceDateTime, request.EventName);
        var response = logs.Select(
                log => new EventLogResponse(
                    log.Name,
                    log.TriggeredAtUtc)
            )
            .ToList();

        return response;
    }
}