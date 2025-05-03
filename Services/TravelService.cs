using Travel_agencies_application.Exceptions;
using Travel_agencies_application.Models;
using Travel_agencies_application.Repositories;

namespace Travel_agencies_application.Services;

public class TravelService(ITravelRepository repository) : ITravelService
{
    public async Task<IEnumerable<TripGetDto>> GetTripsAsync(CancellationToken cancellationToken)
    {
        var result = await repository.GetTripsAsync(cancellationToken);
        return result;
    }

    public async Task<IEnumerable<TripGetByClientIdDTO>> GetTripsByClientIdAsync(int clientId, CancellationToken cancellationToken)
    {
        var result = (await repository.GetTripsByClientIdAsync(clientId, cancellationToken)).ToList();
        
        if (!result.Any())
            throw new NotFoundException("No trips found for the given client.");
        
        return result;
    }

    public async Task<Client> CreateClientAsync(ClientCreateDto client, CancellationToken cancellationToken)
    {
        var result = await repository.CreateClientAsync(client, cancellationToken);

        if (!result.Email.Contains('@') || !result.Email.Contains('.'))
        {
            throw new InvalidFormatException("Invalid email format.");
        }

        if (result.Pesel.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidFormatException("Pesel must only contain digits.");
        }

        if (result.Telephone.StartsWith('+'))
        {
            if (result.Telephone[1..].Any(c => !char.IsDigit(c)))
            {
                throw new InvalidFormatException("Phone number must contain only digits and '+' at the beginning.");
            }
        }
        else
        {
            throw new InvalidFormatException("Phone number must start with '+");
        }
        
        return result;
    }
}