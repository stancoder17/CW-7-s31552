using Microsoft.Data.SqlClient;
using Travel_agencies_application.Exceptions;
using Travel_agencies_application.Models;

namespace Travel_agencies_application.Repositories;

public class DbService(IConfiguration config) : IDbService
{
    private readonly string? _connectionString = config.GetConnectionString("Default");

    /// <summary>
    /// Here a dictionary is used to check if a trip already exists, and if it does, another country is added to that trip.
    /// Instead of getting a list of countries for each trip, the same trip many times is gotten, but each with a different country it's associated with. 
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
    /// The first query is to check if the client exists
    /// </summary>
    /// <returns>All trips for the given client with additional info regarding payment and registration time</returns>
    /// <exception cref="NotFoundException">When the client with the given ID doesn't exist.</exception>
    public async Task<IEnumerable<TripGetByClientIdDTO>> GetTripsByClientIdAsync(int clientId, CancellationToken cancellationToken)
    {
        await using (var connection = new SqlConnection(_connectionString))
        {
            const string clientExistsQuery = @"SELECT 1 FROM Client WHERE IdClient = @ClientId";
            await connection.OpenAsync(cancellationToken);
            await using (var command = new SqlCommand(clientExistsQuery, connection))
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

            const string getQuery = @"SELECT 
                                    t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, ct.RegisteredAt, ct.PaymentDate
                                FROM Trip t
                                JOIN Client_Trip ct ON t.IdTrip = ct.IdTrip 
                                WHERE ct.IdClient = @ClientId";
            await using (var command = new SqlCommand(getQuery, connection))
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
            // If the list is empty, throw an exception.
            if (!result.Any())
                throw new NotFoundException("No trips found for the given client.");
            
            return result;
        }
    }

    /// <summary>
    /// Data format is validated before INSERT.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A new Client</returns>
    /// <exception cref="InvalidFormatException">When the data format is invalid.</exception>
    public async Task<Client> CreateClientAsync(ClientCreateDto client, CancellationToken cancellationToken)
    {
        // Validate data format first
        if (!client.Email.Contains('@') || !client.Email.Contains('.'))
        {
            throw new InvalidFormatException("Invalid email format.");
        }

        if (client.Pesel.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidFormatException("Pesel must only contain digits.");
        }

        if (client.Telephone.StartsWith('+'))
        {
            if (client.Telephone[1..].Any(c => !char.IsDigit(c)))
            {
                throw new InvalidFormatException("Phone number must contain only digits and '+' at the beginning.");
            }
        }
        else
        {
            throw new InvalidFormatException("Phone number must start with '+'.");
        }
        
        // INSERT
        await using (var connection = new SqlConnection(_connectionString))
        {
            const string createClientQuery = @"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
                                   VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel); 
                                   SELECT SCOPE_IDENTITY();"; // The VALUES part works, ignore the error
            await connection.OpenAsync(cancellationToken);
            await using (var command = new SqlCommand(createClientQuery, connection))
            {
                command.Parameters.AddWithValue("@FirstName", client.FirstName);
                command.Parameters.AddWithValue("@LastName", client.LastName);
                command.Parameters.AddWithValue("@Email", client.Email);
                command.Parameters.AddWithValue("@Telephone", client.Telephone);
                command.Parameters.AddWithValue("@Pesel", client.Pesel);

                return new Client
                {
                    IdClient = Convert.ToInt32(
                        await command.ExecuteScalarAsync(cancellationToken) // ExecuteScalarAsync refers to SELECT SCOPE_IDENTITY();
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

    /// <summary>
    /// First the method check if the client and the trip exist and if the existing trip is full.
    /// The INSERT is done when everything is correct.
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="tripId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>A DTO containing Client-Trip data</returns>
    /// <exception cref="NotFoundException">When the client or the trip don't exist</exception>
    /// <exception cref="TripFullException">When the trip is full</exception>
    public async Task<RegisterClientOnTripDTO> RegisterClientOnTripAsync(int clientId, int tripId,
        CancellationToken cancellationToken)
    {
        await using (var connection = new SqlConnection(_connectionString))
        {
            // Check if the client exists
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

            // Check if the trip exists
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
                    throw new TripFullException("This trip is full.");
                }
            }
            
            // Check if the client is already registered on the trip
            const string checkClientOnTripQuery = @"SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip";
            await using (var command = new SqlCommand(checkClientOnTripQuery, connection))
            {
                command.Parameters.AddWithValue("@IdClient", clientId);
                command.Parameters.AddWithValue("@IdTrip", tripId);

                await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                {
                    if (reader.HasRows)
                    {
                        throw new RecordExistsException("Client is already registered on the trip.");
                    }
                }
            }

            // INSERT
            const string insertQuery =
                @"INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate) VALUES (@IdClient, @IdTrip, @RegisteredAt, @PaymentDate);";
            const string getClientData =
                @"SELECT FirstName, LastName, Email, Telephone, Pesel FROM Client WHERE IdClient = @IdClient"; // For DTO data
            const string getTripData =
                @"SELECT Name, Description, DateFrom, DateTo FROM Trip WHERE IdTrip = @IdTrip"; // For DTO data

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
                insertCommand.Parameters.AddWithValue("@RegisteredAt", int.Parse(DateTime.Now.ToString("yyyyMMdd")));
                insertCommand.Parameters.AddWithValue("@PaymentDate", int.Parse(DateTime.Now.ToString("yyyyMMdd")));
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

    /// <summary>
    /// Removes client from the trip
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="tripId"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="NotFoundException">When the trip doesn't exist</exception>
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
                var numOfRows = await command.ExecuteNonQueryAsync(cancellationToken); // To check if the client is registered on the trip.

                if (numOfRows == 0)
                {
                    throw new NotFoundException($"The client with id {clientId} is not registered on the trip with id {tripId}.");
                }
            }
        }
    }
}