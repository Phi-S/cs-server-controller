using Application.ChatCommandFolder;
using ErrorOr;
using Infrastructure.Database;
using MediatR;

namespace Application.CQRS.Commands;

public record DeleteChatCommandCommand(string ChatMessage) : IRequest<ErrorOr<Deleted>>;

public class DeleteChatCommandCommandHandler : IRequestHandler<DeleteChatCommandCommand, ErrorOr<Deleted>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ChatCommandsCache _chatCommandsCache;

    public DeleteChatCommandCommandHandler(UnitOfWork unitOfWork, ChatCommandsCache chatCommandsCache)
    {
        _unitOfWork = unitOfWork;
        _chatCommandsCache = chatCommandsCache;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteChatCommandCommand request,
        CancellationToken cancellationToken)
    {
        var deleteResult = await _unitOfWork.ChatCommandRepo.Delete(request.ChatMessage);
        if (deleteResult.IsError)
        {
            return deleteResult.FirstError;
        }

        await _unitOfWork.Save();
        await _chatCommandsCache.RefreshCache();
        return Result.Deleted;
    }
}