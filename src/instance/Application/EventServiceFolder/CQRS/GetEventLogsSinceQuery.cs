using Infrastructure.Database;
using MediatR;
using Shared.ApiModels;

namespace Application.EventServiceFolder.CQRS;

public record GetEventLogsSinceQuery(string EventName, long LongsSinceUnixMilliseconds)
    : IRequest<List<EventLogResponse>>;

public class GetEventLogsSinceQueryHandler : IRequestHandler<GetEventLogsSinceQuery, List<EventLogResponse>>
{
    private readonly UnitOfWork _unitOfWork;

    public GetEventLogsSinceQueryHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<EventLogResponse>> Handle(
        GetEventLogsSinceQuery request,
        CancellationToken cancellationToken)
    {
        var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(request.LongsSinceUnixMilliseconds).DateTime;
        var logs = await _unitOfWork.EventLogRepo.GetLogsSince(logsSinceDateTime, request.EventName);
        var response = logs.Select(
                log => new EventLogResponse(
                    log.Name,
                    log.TriggeredUtc)
            )
            .ToList();

        return response;
    }
}