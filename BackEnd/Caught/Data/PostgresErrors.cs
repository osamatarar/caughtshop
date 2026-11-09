using Npgsql;

namespace Caught.Data;

internal static class PostgresErrors
{
    public static Exception Map(PostgresException exception)
    {
        return exception.SqlState switch
        {
            PostgresErrorCodes.UniqueViolation => new ApiException(
                StatusCodes.Status409Conflict,
                Text(exception, "That record already exists.")),
            PostgresErrorCodes.NoDataFound => new ApiException(
                StatusCodes.Status404NotFound,
                Text(exception, "The record was not found.")),
            PostgresErrorCodes.InvalidParameterValue => new ApiException(
                StatusCodes.Status400BadRequest,
                Text(exception, "The request is not valid.")),
            PostgresErrorCodes.ForeignKeyViolation => new ApiException(
                StatusCodes.Status400BadRequest,
                "That record is not available."),
            PostgresErrorCodes.CheckViolation => new ApiException(
                StatusCodes.Status400BadRequest,
                "The request is not valid."),
            PostgresErrorCodes.StringDataRightTruncation => new ApiException(
                StatusCodes.Status400BadRequest,
                "The request is not valid."),
            _ => exception,
        };
    }

    public static PostgresException? Find(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is PostgresException postgres)
            {
                return postgres;
            }
        }

        return null;
    }

    private static string Text(PostgresException exception, string fallback)
    {
        return string.IsNullOrWhiteSpace(exception.MessageText) ? fallback : exception.MessageText;
    }
}
