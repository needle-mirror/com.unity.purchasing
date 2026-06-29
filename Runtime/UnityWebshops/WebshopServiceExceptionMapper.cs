using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine.Purchasing.WebshopService.Http;

namespace UnityEngine.Purchasing.WebshopService
{
    internal class WebshopServiceExceptionMapper : IWebshopServiceExceptionMapper
    {
        public async Task<T> InvokeAndMapServiceExceptions<T>(Func<Task<T>> caller)
        {
            try
            {
                return await caller.Invoke();
            }
            catch (HttpException e) when (e.Response.IsNetworkError)
            {
                throw new WebshopException<NetworkError>(
                    $"Could not reach server. {e.Response.ErrorMessage}",
                    new NetworkError());
            }
            catch (ResponseDeserializationException e) when (e.response.IsNetworkError)
            {
                throw new WebshopException<NetworkError>(
                    $"Could not reach server. {e.response.ErrorMessage}",
                    new NetworkError());
            }
            catch (HttpException e) when (e.Response.StatusCode is 400)
            {
                throw new WebshopException<BadRequestError>(
                    "Bad request.", new BadRequestError(e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 401)
            {
                throw new WebshopException<UnauthorizedError>(
                    "Player is not authorized.", new UnauthorizedError(e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 403)
            {
                throw new WebshopException<ForbiddenError>(
                    "Player is forbidden.", new ForbiddenError(e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 404)
            {
                throw new WebshopException<NotFoundError>(
                    $"Could not find resource. Status code {e.Response.StatusCode}.", new NotFoundError(e.Response.StatusCode));
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is 404)
            {
                throw new WebshopException<NotFoundError>(
                    $"Could not find resource. Status code {e.response.StatusCode}.", new NotFoundError(e.response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 409)
            {
                throw new WebshopException<ConflictError>(
                    "Conflict occured.", new ConflictError(e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 422)
            {
                throw new WebshopException<BadRequestError>(
                    "Unprocessable entity.", new BadRequestError(e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 429)
            {
                if (e.Response.Headers.TryGetValue("Retry-After", out var timeInSeconds) &&
                    float.TryParse(timeInSeconds, NumberStyles.Any, CultureInfo.InvariantCulture, out var time))
                {
                    throw new WebshopException<TooManyRequestsError>(
                        $"Request rate limited. Retry after {time} seconds.",
                        new TooManyRequestsError(time, e.Response.StatusCode));
                }
                throw new WebshopException<TooManyRequestsError>(
                    "Request rate limited. Could not retrieve wait time.",
                    new TooManyRequestsError(-1, e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 503)
            {
                throw new WebshopException<ServiceUnavailableError>(
                    $"Could not reach service. Status code {e.Response.StatusCode}.",
                    new ServiceUnavailableError(e.Response.StatusCode));
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is 503)
            {
                throw new WebshopException<ServiceUnavailableError>(
                    $"Could not reach service. Status code {e.response.StatusCode}.",
                    new ServiceUnavailableError(e.response.StatusCode));
            }
            catch (HttpException e)
            {
                throw new WebshopException<HttpError>(
                    $"Status code {e.Response.StatusCode}. Http error: {e.Response.ErrorMessage}",
                    new HttpError(e.Response.StatusCode));
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is not 200)
            {
                throw new WebshopException<ResponseParsingError>(
                    $"Unexpected error format for response code {e.response.StatusCode}. {e.Message}", new ResponseParsingError());
            }
            catch (ResponseDeserializationException e)
            {
                throw new WebshopException<ResponseParsingError>(
                    $"Unexpected response format. {e.Message}", new ResponseParsingError());
            }
            catch (Exception e)
            {
                throw new WebshopException<Exception>($"Unknown error: {e.Message}", e);
            }
        }
    }
}
