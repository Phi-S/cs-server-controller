using ErrorOr;
using Infrastructure.Database;
using MediatR;

namespace Application.CQRS.Commands;

public record DeleteChatCommandCommand(string ChatMessage) : IRequest<ErrorOr<Deleted>>;

public class DeleteChatCommandCommandHandler : IRequestHandler<DeleteChatCommandCommand, ErrorOr<Deleted>>
{
    private readonly UnitOfWork _unitOfWork;

    public DeleteChatCommandCommandHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
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
        return Result.Deleted;
    }
}