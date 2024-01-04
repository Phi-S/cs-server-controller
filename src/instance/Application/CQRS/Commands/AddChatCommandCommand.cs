using Application.ChatCommandsFolder;
using ErrorOr;
using MediatR;

namespace Application.CQRS.Commands;

public record AddChatCommandCommand(string ChatMessage, string Command) : IRequest<ErrorOr<Success>>;

public class AddChatCommandCommandHandler : IRequestHandler<AddChatCommandCommand, ErrorOr<Success>>
{
    private readonly ChatCommandService _chatCommandService;

    public AddChatCommandCommandHandler(ChatCommandService chatCommandService)
    {
        _chatCommandService = chatCommandService;
    }

    public Task<ErrorOr<Success>> Handle(AddChatCommandCommand request, CancellationToken cancellationToken)
    {
        _chatCommandService.AddNewCommand(request.ChatMessage, request.Command);
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }
}