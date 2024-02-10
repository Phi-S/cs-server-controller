using Infrastructure.Database;
using MediatR;
using Shared.ApiModels;

namespace Application.ServerUpdateOrInstallServiceFolder.CQRS;

public record GetServerUpdateOrInstallLogsSinceQuery(long LongsSinceUnixMilliseconds)
    : IRequest<List<UpdateOrInstallLogResponse>>;

public class GetServerUpdateOrInstallLogsSinceQueryHandle : IRequestHandler<GetServerUpdateOrInstallLogsSinceQuery,
    List<UpdateOrInstallLogResponse>>
{
    private readonly UnitOfWork _unitOfWork;

    public GetServerUpdateOrInstallLogsSinceQueryHandle(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<UpdateOrInstallLogResponse>> Handle(GetServerUpdateOrInstallLogsSinceQuery request,
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