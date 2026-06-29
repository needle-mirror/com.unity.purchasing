using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.LiveContentAdapterService
{
    internal static class LiveContentAdapterExceptionExtension
    {
        internal static bool IsRetriable(this LiveContentAdapterException exception)
        {
            return exception switch
            {
                LiveContentAdapterException<ServiceUnavailableError> => true,
                LiveContentAdapterException<NetworkError> => true,
                _ => false
            };
        }
    }

    internal class LiveContentAdapterException : Exception
    {
        internal LiveContentAdapterException(string message) : base(message)
        {
        }
    }

    internal class LiveContentAdapterException<T> : LiveContentAdapterException
    {
        internal T InnerError;

        internal LiveContentAdapterException(string message, T innerError) : base(message)
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
