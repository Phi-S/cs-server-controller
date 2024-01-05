﻿using Infrastructure.Database;
using Infrastructure.Database.Models;
using MediatR;
using Shared.ApiModels;

namespace Application.CQRS.Query;

public record GetAllChatCommandsQuery() : IRequest<List<ChatCommandResponse>>;

public class GetAllChaCommandsQueryHandler : IRequestHandler<GetAllChatCommandsQuery, List<ChatCommandResponse>>
{
    private readonly UnitOfWork _unitOfWork;

    public GetAllChaCommandsQueryHandler(UnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<ChatCommandResponse>> Handle(GetAllChatCommandsQuery request,
        CancellationToken cancellationToken)
    {
        var dbChatCommands = await _unitOfWork.ChatCommandRepo.GetAll();
        return dbChatCommands.Select(dbChatCommand =>
            new ChatCommandResponse(dbChatCommand.Id, dbChatCommand.ChatMessage, dbChatCommand.Command)
        ).ToList();
    }
}