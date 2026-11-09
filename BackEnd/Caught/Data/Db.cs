using Dapper;
using Npgsql;

namespace Caught.Data;

public sealed class Db(NpgsqlDataSource dataSource)
{
    public Task<IReadOnlyList<T>> Query<T>(string sql, object? args, CancellationToken cancellationToken)
    {
        return Call(async () =>
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            var rows = await connection.QueryAsync<T>(new CommandDefinition(sql, args, cancellationToken: cancellationToken));
            return (IReadOnlyList<T>)rows.AsList();
        });
    }

    public Task<T?> QueryOne<T>(string sql, object? args, CancellationToken cancellationToken) where T : class
    {
        return Call(async () =>
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            return await connection.QuerySingleOrDefaultAsync<T>(
                new CommandDefinition(sql, args, cancellationToken: cancellationToken));
        });
    }

    public Task<T> QuerySingle<T>(string sql, object? args, CancellationToken cancellationToken)
    {
        return Call(async () =>
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            return await connection.QuerySingleAsync<T>(
                new CommandDefinition(sql, args, cancellationToken: cancellationToken));
        });
    }

    public Task Execute(string sql, object? args, CancellationToken cancellationToken)
    {
        return Call(async () =>
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            await connection.ExecuteAsync(new CommandDefinition(sql, args, cancellationToken: cancellationToken));
            return true;
        });
    }

    public Task<string?> Scalar(string sql, object? args, CancellationToken cancellationToken)
    {
        return Call(async () =>
        {
            await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
            return await connection.ExecuteScalarAsync<string>(
                new CommandDefinition(sql, args, cancellationToken: cancellationToken));
        });
    }

    private static async Task<T> Call<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception exception) when (PostgresErrors.Find(exception) is PostgresException postgres)
        {
            var mapped = PostgresErrors.Map(postgres);
            if (ReferenceEquals(mapped, postgres))
            {
                throw;
            }

            throw mapped;
        }
    }
}
