using Microsoft.AspNetCore.Mvc;
using Travel_agencies_application.Exceptions;
using Travel_agencies_application.Models;
using Travel_agencies_application.Repositories;

namespace Travel_agencies_application.Controllers;

[ApiController]
[Route("[controller]")]
public class TravelController(IDbService service) : ControllerBase
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

    [HttpPut("clients/{clientId:int}/trips/{tripId:int}")]
    public async Task<IActionResult> RegisterClientOnTrip([FromRoute] int clientId, [FromRoute] int tripId, CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await service.RegisterClientOnTripAsync(clientId, tripId, cancellationToken));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (TripFullException e)
        {
            return BadRequest(e.Message);
        }
        catch (RecordExistsException e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpDelete("clients/{clientId:int}/trips/{tripId:int}")]
    public async Task<IActionResult> RemoveClientFromTrip([FromRoute] int clientId, [FromRoute] int tripId, CancellationToken cancellationToken)
    {
        try
        {
            await service.RemoveClientFromTripAsync(clientId, tripId, cancellationToken);
            return NoContent();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
}