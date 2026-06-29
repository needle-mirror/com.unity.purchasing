using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEngine.Purchasing.LiveContentAdapterService.Http;

namespace UnityEngine.Purchasing.LiveContentAdapterService
{
    internal class LiveContentAdapterServiceExceptionMapper : ILiveContentAdapterServiceExceptionMapper
    {
        public async Task<T> InvokeAndMapServiceExceptions<T>(Func<Task<T>> caller)
        {
            try
            {
                return await caller.Invoke();
            }
            catch (HttpException e) when (e.Response.IsNetworkError)
            {
                throw new LiveContentAdapterException<NetworkError>(
                    $"Could not reach server. {e.Response.ErrorMessage}",
                    new NetworkError());
            }
            catch (ResponseDeserializationException e) when (e.response.IsNetworkError)
            {
                throw new LiveContentAdapterException<NetworkError>(
                    $"Could not reach server. {e.response.ErrorMessage}",
                    new NetworkError());
            }
            catch (HttpException e) when (e.Response.StatusCode is 400)
            {
                throw new LiveContentAdapterException<BadRequestError>(
                    "Bad request.", new BadRequestError(e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 401)
            {
                throw new LiveContentAdapterException<UnauthorizedError>(
                    "Player is not authorized.", new UnauthorizedError(e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 403)
            {
                throw new LiveContentAdapterException<ForbiddenError>(
                    "Player is forbidden.", new ForbiddenError(e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 404)
            {
                throw new LiveContentAdapterException<NotFoundError>(
                    $"Could not find resource. Status code {e.Response.StatusCode}.", new NotFoundError(e.Response.StatusCode));
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is 404)
            {
                throw new LiveContentAdapterException<NotFoundError>(
                    $"Could not find resource. Status code {e.response.StatusCode}.", new NotFoundError(e.response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 409)
            {
                throw new LiveContentAdapterException<ConflictError>(
                    "Conflict occured.", new ConflictError(e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 429)
            {
                if (e.Response.Headers.TryGetValue("Retry-After", out var timeInSeconds) &&
                    float.TryParse(timeInSeconds, NumberStyles.Any, CultureInfo.InvariantCulture, out var time))
                {
                    throw new LiveContentAdapterException<TooManyRequestsError>(
                        $"Request rate limited. Retry after {time} seconds.",
                        new TooManyRequestsError(time, e.Response.StatusCode));
                }
                throw new LiveContentAdapterException<TooManyRequestsError>(
                    "Request rate limited. Could not retrieve wait time.",
                    new TooManyRequestsError(-1, e.Response.StatusCode));
            }
            catch (HttpException e) when (e.Response.StatusCode is 503)
            {
                throw new LiveContentAdapterException<ServiceUnavailableError>(
                    $"Could not reach service. Status code {e.Response.StatusCode}.",
                    new ServiceUnavailableError(e.Response.StatusCode));
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is 503)
            {
                throw new LiveContentAdapterException<ServiceUnavailableError>(
                    $"Could not reach service. Status code {e.response.StatusCode}.",
                    new ServiceUnavailableError(e.response.StatusCode));
            }
            catch (HttpException e)
            {
                throw new LiveContentAdapterException<HttpError>(
                    $"Status code {e.Response.StatusCode}. Http error: {e.Response.ErrorMessage}",
                    new HttpError(e.Response.StatusCode));
            }
            catch (ResponseDeserializationException e) when (e.response.StatusCode is not 200)
            {
                throw new LiveContentAdapterException<ResponseParsingError>(
                    $"Unexpected error format for response code {e.response.StatusCode}. {e.Message}", new ResponseParsingError());
            }
            catch (ResponseDeserializationException e)
            {
                throw new LiveContentAdapterException<ResponseParsingError>(
                    $"Unexpected response format. {e.Message}", new ResponseParsingError());
            }
            catch (Exception e)
            {
                throw new LiveContentAdapterException<Exception>($"Unknown error: {e.Message}", e);
            }
        }
    }
}
