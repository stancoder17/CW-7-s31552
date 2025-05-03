using Microsoft.AspNetCore.Mvc;
using Travel_agencies_application.Models;
using Travel_agencies_application.Services;

namespace Travel_agencies_application.Controllers;

[ApiController]
[Route("[controller]")]
public class TravelController(ITravelService service) : ControllerBase
{
    [HttpGet("trips")]
    public async Task<IEnumerable<TripGetDto>> GetTripsAsync(CancellationToken cancellationToken)
    {
        var result = await service.GetTripsAsync(cancellationToken);
        Console.WriteLine("Returning trips from controller...");
        return result;
    }
}