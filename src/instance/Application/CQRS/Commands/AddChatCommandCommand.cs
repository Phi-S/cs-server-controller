using ErrorOr;
using Infrastructure.Database;
using MediatR;

namespace Application.CQRS.Commands;

public record AddChatCommandCommand(string ChatMessage, string ServerCommand) : IRequest<ErrorOr<Success>>;

public class AddChatCommandCommandHandler : IRequestHandler<AddChatCommandCommand, ErrorOr<Success>>
{
    private readonly UnitOfWork _unitOfWork;

    public AddChatCommandCommandHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<Success>> Handle(AddChatCommandCommand request, CancellationToken cancellationToken)
    {
        var addResult = await _unitOfWork.ChatCommandRepo.Add(request.ChatMessage, request.ServerCommand);
        if (addResult.IsError)
        {
            return addResult.FirstError;
        }

        await _unitOfWork.Save();
        return Result.Success;
    }
}