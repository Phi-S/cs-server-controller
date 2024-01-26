﻿using Infrastructure.Database;
using MediatR;
using Shared.ApiModels;

namespace Application.UpdateOrInstallServiceFolder.CQRS;

public record GetUpdateOrInstallLogsSinceQuery(long LongsSinceUnixMilliseconds)
    : IRequest<List<UpdateOrInstallLogResponse>>;

public class
    GetUpdateOrInstallLogsSinceQueryHandle : IRequestHandler<GetUpdateOrInstallLogsSinceQuery,
    List<UpdateOrInstallLogResponse>>
{
    private readonly UnitOfWork _unitOfWork;

    public GetUpdateOrInstallLogsSinceQueryHandle(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UpdateOrInstallLogResponse>> Handle(GetUpdateOrInstallLogsSinceQuery request,
        CancellationToken cancellationToken)
    {
        var logsSinceDateTime = DateTimeOffset.FromUnixTimeMilliseconds(request.LongsSinceUnixMilliseconds).DateTime;
        var logs = await _unitOfWork.UpdateOrInstallRepo.GetLogsSince(logsSinceDateTime);
        var response = logs.Select(
                log => new UpdateOrInstallLogResponse(
                    log.UpdateOrInstallStartDbModel.Id,
                    log.Message,
                    log.CreatedUtc)
            )
            .ToList();
        return response;
    }
}