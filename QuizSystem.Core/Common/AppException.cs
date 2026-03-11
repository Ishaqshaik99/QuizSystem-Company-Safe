using System.Net;

namespace QuizSystem.Core.Common;

public class AppException : Exception
{
    public AppException(string message, HttpStatusCode statusCode = HttpStatusCode.BadRequest)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
