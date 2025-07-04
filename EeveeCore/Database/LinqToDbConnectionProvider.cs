namespace EeveeCore.Database;

/// <summary>
///     Provides a service for managing and retrieving instances of DittoDataConnection.
///     This class implements INService to integrate with the application's service architecture.
/// </summary>
/// <remarks>
///     This provider creates new instances of DittoDataConnection for LinqToDB operations,
///     replacing the Entity Framework DbContextProvider.
/// </remarks>
public class LinqToDbConnectionProvider 
{
    private readonly string _connectionString;

    /// <summary>
    ///     Initializes a new instance of the <see cref="LinqToDbConnectionProvider" /> class.
    /// </summary>
    public LinqToDbConnectionProvider(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    ///     Creates a new instance of DittoDataConnection.
    /// </summary>
    /// <returns>A new instance of <see cref="DittoDataConnection" />.</returns>
    /// <remarks>
    ///     This method creates a new connection instance each time it's called.
    ///     The connection will be automatically disposed when the using block ends.
    /// </remarks>
    public DittoDataConnection GetConnection()
    {
        return new DittoDataConnection(_connectionString);
    }

    /// <summary>
    ///     Asynchronously creates a new instance of DittoDataConnection.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation.
    ///     The task result contains a new instance of <see cref="DittoDataConnection" />.
    /// </returns>
    /// <remarks>
    ///     This method provides async compatibility with existing code patterns.
    ///     The connection will be automatically disposed when the using block ends.
    /// </remarks>
    public Task<DittoDataConnection> GetConnectionAsync()
    {
        return Task.FromResult(new DittoDataConnection(_connectionString));
    }
}