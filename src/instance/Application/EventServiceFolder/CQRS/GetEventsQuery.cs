using MediatR;

namespace Application.EventServiceFolder.CQRS;

public record GetEventsQuery : IRequest<List<string>>;

public class GetEventsQueryHandler : IRequestHandler<GetEventsQuery, List<string>>
{
    public Task<List<string>> Handle(GetEventsQuery request, CancellationToken cancellationToken)
    {
        var events = Enum.GetValues<Events>().Select(e => e.ToString()).ToList();
        return Task.FromResult(events);
    }
}