using Microsoft.AspNetCore.Mvc;

namespace EventManagement.Api.Common;

/// <summary>Maps business <see cref="Error"/>s to RFC 9457 problem-details responses.</summary>
public static class ResultExtensions
{
    public static ActionResult ToProblem(this ControllerBase controller, Error error)
    {
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problem = controller.ProblemDetailsFactory.CreateProblemDetails(
            controller.HttpContext,
            statusCode: status,
            detail: error.Message);
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem) { StatusCode = status };
    }
}
