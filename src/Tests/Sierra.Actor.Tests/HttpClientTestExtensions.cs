using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

/// <summary>
/// some test level useful methods for http client 
/// </summary>
// ReSharper disable once CheckNamespace
public static class HttpClientTestExtensions
{
    /// <summary>
    /// send request to the test middleware with JSON payload and set actor and method name
    /// </summary>
    /// <typeparam name="T">payload type</typeparam>
    /// <param name="cl">http client instance</param>
    /// <param name="baseUri">base urk for the middleware</param>
    /// <param name="actorName">actor name</param>
    /// <param name="method">method name - Add or Remove</param>
    /// <param name="payload">payload instance</param>
    /// <param name="checkResponseCode">flag to indicate whether response code should be checked</param>
    /// <returns>response message</returns>
    public static async Task<T> PostJsonToActor<T>(this HttpClient cl, string baseUri, string actorName,
        string method, T payload, bool checkResponseCode = true) 
        where T : class       
    {
        var response = await cl.PostAsJsonAsync($"{baseUri}/{actorName}/{method}", payload);

        if (checkResponseCode)
            response.EnsureSuccessStatusCode();

        if (response.StatusCode == HttpStatusCode.OK)
        {
            return await response.Content.ReadAsAsync<T>();
        }

        return null;
    }
}
