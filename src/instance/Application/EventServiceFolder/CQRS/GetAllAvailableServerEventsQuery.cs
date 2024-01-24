using MediatR;

namespace Application.EventServiceFolder.CQRS;

public record GetAllAvailableServerEventsQuery() : IRequest<List<string>>;

public class GetAllAvailableServerEventsQueryHandler : IRequestHandler<GetAllAvailableServerEventsQuery, List<string>>
{
    public Task<List<string>> Handle(GetAllAvailableServerEventsQuery request, CancellationToken cancellationToken)
    {
        var events = Enum.GetValues<Events>().Select(e => e.ToString()).ToList();
        return Task.FromResult(events);
    }
}