using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Purchasing.PaymentProviderService.Http;
using UnityEngine.Purchasing.PaymentProviderService.Models;

namespace UnityEngine.Purchasing.PaymentProviderService
{
    internal class PaymentProviderServiceExceptionMapper : IPaymentProviderServiceExceptionMapper
    {
        public async Task<T> InvokeAndMapServiceExceptions<T>(Func<Task<T>> caller)
        {
            try
            {
                return await caller.Invoke();
            }
            catch (HttpException e) when (e.Response.IsNetworkError)
            {
                var error = new NetworkError();
                throw new PaymentProviderException<NetworkError>(
                    $"Could not reach server. {e.Response.ErrorMessage}",
                    error);
            }
            catch (ResponseDeserializationException e) when (e.response.IsNetworkError)
            {
                var error = new NetworkError();
                throw new PaymentProviderException<NetworkError>(
                    $"Could not reach server. {e.response.ErrorMessage}",
                    error);
            }
            catch (HttpException<ValidationErrorResponse> e)
            {
                var fieldErrors = e.ActualError?.Errors?.Select(
                        error => new FieldValidationErrors(error.Field, error.Messages)
                        ).ToList() ?? new List<FieldValidationErrors>();

                var validationError = new ValidationError(fieldErrors, e.Response.StatusCode);

                var errorMessage = $"Validation error with status code {e.Response.StatusCode}"
                                   + $" for fields: {ConcatenateValidationFields(validationError)}"
                                   + $"\n{ConcatenateValidationFieldsWithMessages(validationError)}.";

                throw new PaymentProviderException<ValidationError>(errorMessage, validationError);
            }
            catch (HttpException e) when (e.Response.StatusCode is 400)
            {
                var error = new BadRequestError(e.Response.StatusCode);
                throw new PaymentProviderException<BadRequestError>(
                    "Bad request.", error);
            }
            catch (HttpException<BasicErrorResponse> e) when (e.Response.StatusCode is 401)
            {
                var error = new UnauthorizedError(e.Response.StatusCode);
                throw new PaymentProviderException<UnauthorizedError>(
                    $"Player is not authorized. {e.ActualError.Detail}", error);
            }
            catch (HttpException<BasicErrorResponse> e) when (e.Response.StatusCode is 403)
            {
                var error = new ForbiddenError(e.Response.StatusCode);
                throw new PaymentProviderException<ForbiddenError>(
                    $"Player is not authorized. {e.ActualError.Detail}", error);
            }
            catch (HttpException e) when (e.Response.StatusCode is 404)
            {
                var error = new NotFoundError(e.Response.StatusCode);
                throw new PaymentProviderException<NotFoundError>(
                    $"Could not find resource. Status code {e.Response.StatusCode}.", error);
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is 404)
            {
                var error = new NotFoundError(e.response.StatusCode);
                throw new PaymentProviderException<NotFoundError>(
                    $"Could not find resource. Status code {e.response.StatusCode}.", error);
            }
            catch (HttpException<BasicErrorResponse> e) when (e.Response.StatusCode is 409)
            {
                var error = new ConflictError(e.Response.StatusCode);
                throw new PaymentProviderException<ConflictError>(
                    $"Conflict occured. {e.ActualError.Detail}", error);
            }
            catch (HttpException e) when (e.Response.StatusCode is 409)
            {
                var error = new ConflictError(e.Response.StatusCode);
                throw new PaymentProviderException<ConflictError>(
                    $"Conflict occured.", error);
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is 409)
            {
                var error = new ConflictError(e.response.StatusCode);
                throw new PaymentProviderException<ConflictError>(
                    $"Conflict occured. Status code {e.response.StatusCode}.", error);
            }
            catch (HttpException e) when (e.Response.StatusCode is 429)
            {
                if (e.Response.Headers.TryGetValue("Retry-After", out var timeInSeconds) &&
                    float.TryParse(timeInSeconds, NumberStyles.Any, CultureInfo.InvariantCulture, out var time))
                {
                    var error = new TooManyRequestsError(time, e.Response.StatusCode);
                    throw new PaymentProviderException<TooManyRequestsError>(
                        $"Request rate limited. Retry after {time} seconds.", error);
                }
                else
                {
                    var error = new TooManyRequestsError(-1, e.Response.StatusCode);
                    throw new PaymentProviderException<TooManyRequestsError>(
                        "Request rate limited. Could not retrieve wait time.", error);
                }
            }
            catch (HttpException e) when (e.Response.StatusCode is 503)
            {
                var error = new ServiceUnavailableError(e.Response.StatusCode);
                throw new PaymentProviderException<ServiceUnavailableError>(
                    $"Could not reach service. Status code {e.Response.StatusCode}.", error);
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is 503)
            {
                var error = new ServiceUnavailableError(e.response.StatusCode);
                throw new PaymentProviderException<ServiceUnavailableError>(
                    $"Could not reach service. Status code {e.response.StatusCode}.", error);
            }
            catch (HttpException<BasicErrorResponse> e)
            {
                var error = new HttpError(e.Response.StatusCode);
                throw new PaymentProviderException<HttpError>(
                    $"Status code {e.Response.StatusCode}. Error: {e.ActualError.Detail}", error);
            }
            catch (HttpException e)
            {
                throw new PaymentProviderException($"{e.Response.StatusCode} Http error: {e.Response.ErrorMessage}");
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is not 200)
            {
                throw new PaymentProviderException<ResponseParsingError>(
                    $"Unexpected error format for response code {e.response.StatusCode}. {e.Message}", new ResponseParsingError());
            }
            catch (ResponseDeserializationException e)
            {
                throw new PaymentProviderException<ResponseParsingError>(
                    $"Unexpected response format. {e.Message}", new ResponseParsingError());
            }
            catch (Exception e)
            {
                throw new PaymentProviderException<Exception>($"Unknown error: {e.Message}", e);
            }
        }

        #region Helpers

        #region ValidationError Message Helpers

        internal string GetValidationField(FieldValidationErrors error)
        {
            return error.Field ?? "field name not found";
        }

        internal string ConcatenateValidationFields(ValidationError error)
        {
            return string.Join(
                ", ",
                error.Errors?.Select(GetValidationField) ?? Enumerable.Empty<string>()
                );
        }

        internal string ConcatenateValidationFieldsWithMessages(ValidationError error)
        {
            return string.Join(
                '\n',
                error.Errors?.Select(
                    e => $"{GetValidationField(e)} | {ConcatenateFieldValidationMessages(e)}"
                    ) ?? Enumerable.Empty<string>()
                );
        }

        internal string ConcatenateFieldValidationMessages(FieldValidationErrors error)
        {
            var result = string.Join(", ", error.Messages ?? Enumerable.Empty<string>());
            return string.IsNullOrWhiteSpace(result) ? "No validation messages provided." : result;
        }
        #endregion

        #endregion
    }
}
