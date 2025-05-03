using Microsoft.AspNetCore.Mvc;
using Travel_agencies_application.Exceptions;
using Travel_agencies_application.Models;
using Travel_agencies_application.Services;

namespace Travel_agencies_application.Controllers;

[ApiController]
[Route("[controller]")]
public class TravelController(ITravelService service) : ControllerBase
{
    [HttpGet("trips")]
    public async Task<IActionResult> GetTrips(CancellationToken cancellationToken)
    {
        return Ok(await service.GetTripsAsync(cancellationToken));
    }

    [HttpGet("clients/{clientId:int}/trips")]
    public async Task<IActionResult> GetTripsByClientId([FromRoute] int clientId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.GetTripsByClientIdAsync(clientId, cancellationToken));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateClient([FromBody] ClientCreateDto body, CancellationToken cancellationToken)
    {
        try
        {
            var client = await service.CreateClientAsync(body, cancellationToken);
            return Created(string.Empty, $"Added new client with id: {client.IdClient}");
        }
        catch (InvalidFormatException e)
        {
            return BadRequest(e.Message);
        }
    }
}