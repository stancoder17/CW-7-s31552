using Microsoft.Data.SqlClient;
using Travel_agencies_application.Models;

namespace Travel_agencies_application.Repositories;

public class TravelRepository(IConfiguration config) : ITravelRepository
{
    private readonly string? _connectionString = config.GetConnectionString("Default");

    /// <summary>
    /// Here I'm using dictionary to check if a trip already exists, and if it does I add another country to that trip.
    /// I do it, because instead of getting a list of countries for each trip, I get the same trip many times, but each with different country it's associated with. 
    /// </summary>
    public async Task<IEnumerable<TripGetDto>> GetTripsAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("Getting trips from database...");
        var result = new Dictionary<int, TripGetDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        const string sql = @"SELECT 
                                    t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName
                                FROM Trip t
                                JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                                JOIN Country c ON ct.IdCountry = c.IdCountry";
        await using (var command = new SqlCommand(sql, connection)) {
            await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    var idTrip = reader.GetInt32(reader.GetOrdinal("IdTrip"));
                    if (!result.TryGetValue(idTrip, out var trip))
                    {
                        trip = new TripGetDto
                        {
                            IdTrip = idTrip,
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                            DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                            MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                            Countries = []
                        };
                        result.Add(idTrip, trip);
                    }
                    var country = reader.GetString(reader.GetOrdinal("CountryName"));
                    if (!trip.Countries.Contains(country))
                    {
                        trip.Countries.Add(country);
                    }
                }
            }
        }
        return result
            .OrderBy(entry => entry.Key)
            .Select(entry => entry.Value)
            .ToList();
    }
}