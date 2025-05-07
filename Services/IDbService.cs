using Travel_agencies_application.Models;

namespace Travel_agencies_application.Repositories;

public interface IDbService
{
    public Task<IEnumerable<TripGetDto>> GetTripsAsync(CancellationToken cancellationToken);
    public Task<IEnumerable<TripGetByClientIdDTO>> GetTripsByClientIdAsync(int clientId, CancellationToken cancellationToken);
    public Task<Client> CreateClientAsync(ClientCreateDto client, CancellationToken cancellationToken);
    public Task<RegisterClientOnTripDTO> RegisterClientOnTripAsync(int clientId, int tripId, CancellationToken cancellationToken);
    public Task RemoveClientFromTripAsync(int clientId, int tripId, CancellationToken cancellationToken);
}