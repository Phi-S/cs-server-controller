﻿using Infrastructure.Database;
using MediatR;
using Shared.ApiModels;

namespace Application.ServerServiceFolder.CQRS;

public record GetServerLogsSinceQuery(long LongsSinceUnixMilliseconds) : IRequest<List<ServerLogResponse>>;

public class GetServerLogsSinceQueryHandler : IRequestHandler<GetServerLogsSinceQuery, List<ServerLogResponse>>
{
    private readonly UnitOfWork _unitOfWork;

    public GetServerLogsSinceQueryHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<List<ServerLogResponse>> Handle(GetServerLogsSinceQuery request, CancellationToken cancellationToken)
    {
        var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(request.LongsSinceUnixMilliseconds).DateTime;
        var logs = await _unitOfWork.ServerRepo.GetLogsSince(logsSinceDateTime);
        var response = logs.Select(
                log => new ServerLogResponse(
                    log.ServerStartDbModel.Id,
                    log.Message,
                    log.CreatedUtc)
            )
            .ToList();

        return response;
    }
}