using Travel_agencies_application.Models;

namespace Travel_agencies_application.Services;

public interface ITravelService
{
    public Task<IEnumerable<TripGetDto>> GetTripsAsync(CancellationToken cancellationToken);
    public Task<IEnumerable<TripGetByClientIdDTO>> GetTripsByClientIdAsync(int clientId, CancellationToken cancellationToken);
    public Task<Client> CreateClientAsync(ClientCreateDto client, CancellationToken cancellationToken);

}