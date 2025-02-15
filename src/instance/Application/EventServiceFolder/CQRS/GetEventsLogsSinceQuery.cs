﻿using Infrastructure.Database;
using MediatR;
using Shared.ApiModels;

namespace Application.EventServiceFolder.CQRS;

public record GetEventsLogsSinceQuery(long LongsSinceUnixMilliseconds) : IRequest<List<EventLogResponse>>;

public class GetEventsLogsSinceQueryHandler : IRequestHandler<GetEventsLogsSinceQuery, List<EventLogResponse>>
{
    private readonly UnitOfWork _unitOfWork;

    public GetEventsLogsSinceQueryHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<EventLogResponse>> Handle(GetEventsLogsSinceQuery request,
        CancellationToken cancellationToken)
    {
        var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(request.LongsSinceUnixMilliseconds).DateTime;
        var logs = await _unitOfWork.EventLogRepo.GetLogsSince(logsSinceDateTime);
        var response = logs.Select(
                log => new EventLogResponse(
                    log.Name,
                    log.TriggeredUtc)
            )
            .ToList();

        return response;
    }
}