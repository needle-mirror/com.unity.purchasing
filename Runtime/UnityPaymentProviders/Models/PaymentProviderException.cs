using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.PaymentProviderService
{
    internal static class PaymentProviderExceptionExtension
    {
        internal static bool IsRetriable(this PaymentProviderException exception)
        {
            return exception switch
            {
                PaymentProviderException<ServiceUnavailableError> => true,
                PaymentProviderException<NetworkError> => true,
                _ => false
            };
        }
    }

    internal class PaymentProviderException : Exception
    {
        internal PaymentProviderException(string message) : base(message)
        {
        }
    }

    internal class PaymentProviderException<T> : PaymentProviderException
    {
        internal T InnerError;

        internal PaymentProviderException(string message, T innerError) : base(message)
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

    internal class ValidationError : HttpError
    {
        internal readonly IReadOnlyList<FieldValidationErrors> Errors;

        internal ValidationError(IReadOnlyList<FieldValidationErrors> errors, long responseCode) : base(responseCode)
        {
            Errors = errors;
        }
    }

    internal class FieldValidationErrors
    {
        internal readonly string Field;
        internal readonly IReadOnlyList<string> Messages;

        internal FieldValidationErrors(string field, IReadOnlyList<string> messages)
        {
            Field = field;
            Messages = messages;
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

    // could merge into a NotAccessible error, but there are a finite number of http error codes.
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
