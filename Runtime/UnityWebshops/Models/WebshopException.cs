using System;

namespace UnityEngine.Purchasing.WebshopService
{
    internal static class WebshopExceptionExtension
    {
        internal static bool IsRetriable(this WebshopException exception)
        {
            return exception switch
            {
                WebshopException<ServiceUnavailableError> => true,
                WebshopException<NetworkError> => true,
                _ => false
            };
        }
    }

    internal class WebshopException : Exception
    {
        internal WebshopException(string message) : base(message)
        {
        }
    }

    internal class WebshopException<T> : WebshopException
    {
        internal T InnerError;

        internal WebshopException(string message, T innerError) : base(message)
        {
            InnerError = innerError;
        }
    }

    internal class HttpError
    {
        internal long ResponseCode { get; }

        internal HttpError(long responseCode)
        {
            ResponseCode = responseCode;
        }
    }

    internal class TooManyRequestsError : HttpError
    {
        internal readonly float SecondsToRetry;

        internal TooManyRequestsError(float secondsToRetry, long responseCode) : base(responseCode)
        {
            SecondsToRetry = secondsToRetry;
        }
    }

    internal class BadRequestError : HttpError
    {
        internal BadRequestError(long responseCode) : base(responseCode)
        { }
    }

    internal class ServiceUnavailableError : HttpError
    {
        internal ServiceUnavailableError(long responseCode) : base(responseCode)
        { }
    }

    internal class NotFoundError : HttpError
    {
        internal NotFoundError(long responseCode) : base(responseCode)
        { }
    }

    internal class UnauthorizedError : HttpError
    {
        internal UnauthorizedError(long responseCode) : base(responseCode)
        { }
    }

    internal class ForbiddenError : HttpError
    {
        internal ForbiddenError(long responseCode) : base(responseCode)
        { }
    }

    internal class ConflictError : HttpError
    {
        internal ConflictError(long responseCode) : base(responseCode)
        { }
    }

    internal class NetworkError
    {
    }

    internal class ResponseParsingError
    {
    }
}
