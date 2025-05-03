using Travel_agencies_application.Models;

namespace Travel_agencies_application.Repositories;

public interface ITravelRepository
{
    public Task<IEnumerable<TripGetDto>> GetTripsAsync(CancellationToken cancellationToken);
}