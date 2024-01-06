using Application.ChatCommandFolder;
using ErrorOr;
using Infrastructure.Database;
using MediatR;

namespace Application.CQRS.Commands;

public record AddChatCommandCommand(string ChatMessage, string ServerCommand) : IRequest<ErrorOr<Success>>;

public class AddChatCommandCommandHandler : IRequestHandler<AddChatCommandCommand, ErrorOr<Success>>
{
    private readonly UnitOfWork _unitOfWork;
    private readonly ChatCommandsCache _chatCommandsCache;

    public AddChatCommandCommandHandler(UnitOfWork unitOfWork, ChatCommandsCache chatCommandsCache)
    {
        _unitOfWork = unitOfWork;
        _chatCommandsCache = chatCommandsCache;
    }

    public async Task<ErrorOr<Success>> Handle(AddChatCommandCommand request, CancellationToken cancellationToken)
    {
        var addResult = await _unitOfWork.ChatCommandRepo.Add(request.ChatMessage, request.ServerCommand);
        if (addResult.IsError)
        {
            return addResult.FirstError;
        }

        await _unitOfWork.Save();
        await _chatCommandsCache.RefreshCache();
        return Result.Success;
    }
}