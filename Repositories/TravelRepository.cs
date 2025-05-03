using Microsoft.Data.SqlClient;
using Travel_agencies_application.Exceptions;
using Travel_agencies_application.Models;

namespace Travel_agencies_application.Repositories;

public class TravelRepository(IConfiguration config) : ITravelRepository
{
    private readonly string? _connectionString = config.GetConnectionString("Default");

    /// <summary>
    /// Here I'm using dictionary to check if a trip already exists, and if it does I add another country to that trip.
    /// I do it, because instead of getting a list of countries for each trip, I get the same trip many times, but each with different country it's associated with. 
    /// </summary>
    /// <returns>List of all trips plus country names.</returns>
    public async Task<IEnumerable<TripGetDto>> GetTripsAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<int, TripGetDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        const string query = @"SELECT 
                                    t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName
                                FROM Trip t
                                JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                                JOIN Country c ON ct.IdCountry = c.IdCountry";
        await using (var command = new SqlCommand(query, connection))
        {
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

    /// <summary>
    /// First query is to check if the client exists
    /// </summary>
    /// <returns>All trips for the given client with additional info regarding payment and registration time</returns>
    /// <exception cref="NotFoundException">When the client with the given ID doesn't exist.</exception>
    public async Task<IEnumerable<TripGetByClientIdDTO>> GetTripsByClientIdAsync(int clientId,
        CancellationToken cancellationToken)
    {
        await using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"SELECT 1 FROM Client WHERE IdClient = @ClientId";
            await connection.OpenAsync(cancellationToken);
            await using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ClientId", clientId);
                await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (!reader.HasRows)
                    {
                        throw new NotFoundException($"Client with id {clientId} does not exist.");
                    }
                }
            }

            var result = new List<TripGetByClientIdDTO>();

            const string query2 = @"SELECT 
                                    t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate
                                FROM Trip t
                                JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip 
                                WHERE ct.IdClient = @ClientId";
            await using (var command = new SqlCommand(query2, connection))
            {
                command.Parameters.AddWithValue("@ClientId", clientId);

                await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var clientTrip = new TripGetByClientIdDTO
                        {
                            IdTrip = reader.GetInt32(reader.GetOrdinal("IdTrip")),
                            TripName = reader.GetString(reader.GetOrdinal("Name")),
                            Description = reader.GetString(reader.GetOrdinal("Description")),
                            DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                            DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                            MaxPeople = reader.GetInt32(reader.GetOrdinal("MaxPeople")),
                            RegisteredAt = reader.GetInt32(reader.GetOrdinal("RegisteredAt")),
                            PaymentDate = reader.GetInt32(reader.GetOrdinal("PaymentDate"))
                        };
                        result.Add(clientTrip);
                    }
                }
            }

            return result;
        }
    }

    public async Task<Client> CreateClientAsync(ClientCreateDto client, CancellationToken cancellationToken)
    {
        await using (var connection = new SqlConnection(_connectionString))
        {
            const string query = @"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                                   VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel); 
                                   SELECT SCOPE_IDENTITY();"; // The VALUES part works, ignore the error
            await connection.OpenAsync(cancellationToken);
            await using (var command = new SqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@FirstName", client.FirstName);
                command.Parameters.AddWithValue("@LastName", client.LastName);
                command.Parameters.AddWithValue("@Email", client.Email);
                command.Parameters.AddWithValue("@Telephone", client.Telephone);
                command.Parameters.AddWithValue("@Pesel", client.Pesel);

                return new Client
                {
                    IdClient = Convert.ToInt32(
                        await command.ExecuteScalarAsync(
                            cancellationToken) // ExecScalAsync refers to SELECT SCOPE_IDENTITY();
                    ),
                    FirstName = client.FirstName,
                    LastName = client.LastName,
                    Email = client.Email,
                    Telephone = client.Telephone,
                    Pesel = client.Pesel,
                };
            }
        }
    }

    public async Task<RegisterClientOnTripDTO> RegisterClientOnTripAsync(int clientId, int tripId,
        CancellationToken cancellationToken)
    {
        await using (var connection = new SqlConnection(_connectionString))
        {
            // Check if client exists
            const string getClientQuery = @"SELECT 1 FROM Client WHERE IdClient = @ClientId";
            await connection.OpenAsync(cancellationToken);
            await using (var command = new SqlCommand(getClientQuery, connection))
            {
                command.Parameters.AddWithValue("@ClientId", clientId);
                await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (!reader.HasRows)
                    {
                        throw new NotFoundException($"Client with id {clientId} does not exist.");
                    }
                }
            }

            // Check if trip exists
            const string getTripQuery = @"SELECT 1 FROM Trip WHERE IdTrip = @TripId";
            await using (var command = new SqlCommand(getTripQuery, connection))
            {
                command.Parameters.AddWithValue("@TripId", tripId);
                await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (!reader.HasRows)
                    {
                        throw new NotFoundException($"Trip with id {tripId} does not exist.");
                    }
                }
            }

            // Check if the trip is full
            const string getMaxPeopleQuery = @"SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip";
            const string getPeopleCountQuery = @"SELECT Count(*) FROM Client_Trip WHERE IdTrip = @IdTrip";
            int maxPeople;
            int peopleCount;

            await using (var command = new SqlCommand(getMaxPeopleQuery, connection))
            {
                command.Parameters.AddWithValue("@IdTrip", tripId);
                maxPeople = Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
            }

            await using (var command2 = new SqlCommand(getPeopleCountQuery, connection))
            {
                command2.Parameters.AddWithValue("@IdTrip", tripId);
                peopleCount = Convert.ToInt32(await command2.ExecuteScalarAsync(cancellationToken));

                if (peopleCount == maxPeople)
                {
                    throw new TripFullException("Too many people on this trip.");
                }
            }

            // Everything is correct - INSERT
            const string insertQuery =
                @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate) VALUES (@IdClient, @IdTrip, @RegisteredAt, @PaymentDate);";
            const string getClientData =
                @"SELECT FirstName, LastName, Email, Telephone, Pesel FROM Client WHERE IdClient = @IdClient";
            const string getTripData = @"SELECT Name, Description, DateFrom, DateTo FROM Trip WHERE IdTrip = @IdTrip";

            string firstName, lastName, email, telephone, pesel;
            string tripName, tripDescription;
            DateTime dateFrom, dateTo;

            await using (var clientDataCommand = new SqlCommand(getClientData, connection))
            {
                clientDataCommand.Parameters.AddWithValue("@IdClient", clientId);
                await using var clientDataReader = await clientDataCommand.ExecuteReaderAsync(cancellationToken);
                await clientDataReader.ReadAsync(cancellationToken);
                firstName = clientDataReader.GetString(clientDataReader.GetOrdinal("FirstName"));
                lastName = clientDataReader.GetString(clientDataReader.GetOrdinal("LastName"));
                email = clientDataReader.GetString(clientDataReader.GetOrdinal("Email"));
                telephone = clientDataReader.GetString(clientDataReader.GetOrdinal("Telephone"));
                pesel = clientDataReader.GetString(clientDataReader.GetOrdinal("Pesel"));
            }

            await using (var tripDataCommand = new SqlCommand(getTripData, connection))
            {
                tripDataCommand.Parameters.AddWithValue("@IdTrip", tripId);
                await using var tripDataReader = await tripDataCommand.ExecuteReaderAsync(cancellationToken);
                await tripDataReader.ReadAsync(cancellationToken);
                tripName = tripDataReader.GetString(tripDataReader.GetOrdinal("Name"));
                tripDescription = tripDataReader.GetString(tripDataReader.GetOrdinal("Description"));
                dateFrom = tripDataReader.GetDateTime(tripDataReader.GetOrdinal("DateFrom"));
                dateTo = tripDataReader.GetDateTime(tripDataReader.GetOrdinal("DateTo"));
            }

            await using (var insertCommand = new SqlCommand(insertQuery, connection))
            {
                insertCommand.Parameters.AddWithValue("@IdClient", clientId);
                insertCommand.Parameters.AddWithValue("@IdTrip", tripId);
                insertCommand.Parameters.AddWithValue("@RegisteredAt", DateTime.Now);
                insertCommand.Parameters.AddWithValue("@PaymentDate", DateTime.Now);
                await insertCommand.ExecuteNonQueryAsync(cancellationToken);
            }

            return new RegisterClientOnTripDTO
            {
                IdClient = clientId,
                IdTrip = tripId,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                Telephone = telephone,
                Pesel = pesel,
                TripName = tripName,
                TripDescription = tripDescription,
                DateFrom = dateFrom,
                DateTo = dateTo,
            };
        }
    }

    public async Task RemoveClientFromTripAsync(int clientId, int tripId, CancellationToken cancellationToken)
    {
        await using (var connection = new SqlConnection(_connectionString))
        {
            const string deleteQuery = @"DELETE FROM Client_Trip WHERE idClient = @IdClient AND IdTrip = @IdTrip";
            await connection.OpenAsync(cancellationToken);
            await using (var command = new SqlCommand(deleteQuery, connection))
            {
                command.Parameters.AddWithValue("@IdClient", clientId);
                command.Parameters.AddWithValue("@IdTrip", tripId);
                var numOfRows = await command.ExecuteNonQueryAsync(cancellationToken);

                if (numOfRows == 0)
                {
                    throw new NotFoundException($"Trip not found.");
                }
            }
        }
    }
}