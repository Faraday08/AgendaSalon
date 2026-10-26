using AgendaSalon.Application.Comun;
using AgendaSalon.Domain.Comun;
using Microsoft.AspNetCore.Diagnostics;

namespace AgendaSalon.Api;

/// <summary>Convierte los errores de negocio en respuestas ProblemDetails con el código HTTP adecuado.</summary>
internal sealed class ManejadorExcepciones(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        int? codigo = exception switch
        {
            ReglaNegocioException => StatusCodes.Status400BadRequest,
            NoAutorizadoException => StatusCodes.Status401Unauthorized,
            ProhibidoException => StatusCodes.Status403Forbidden,
            NoEncontradoException => StatusCodes.Status404NotFound,
            ConflictoException => StatusCodes.Status409Conflict,
            _ => null
        };

        if (codigo is null)
            return false;

        httpContext.Response.StatusCode = codigo.Value;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Title = exception.Message, Status = codigo }
        });
    }
}
