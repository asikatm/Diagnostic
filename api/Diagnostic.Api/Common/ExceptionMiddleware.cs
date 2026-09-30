using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Diagnostic.Api.Common;

/// <summary>Turns exceptions into a consistent JSON body: <c>{ "message": "..." }</c>.</summary>
public class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            await Write(context, ex.StatusCode, ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        {
            await Write(context, StatusCodes.Status409Conflict, "Duplicate value – a record with the same code / name already exists.");
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 547 })
        {
            await Write(context, StatusCodes.Status409Conflict, "This record is referenced by other data and cannot be changed this way.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled error on {Path}", context.Request.Path);
            await Write(context, StatusCodes.Status500InternalServerError, "Unexpected server error. Please contact the administrator.");
        }
    }

    private static Task Write(HttpContext context, int status, string message)
    {
        if (context.Response.HasStarted) return Task.CompletedTask;
        context.Response.Clear();
        context.Response.StatusCode = status;
        return context.Response.WriteAsJsonAsync(new { message });
    }
}
