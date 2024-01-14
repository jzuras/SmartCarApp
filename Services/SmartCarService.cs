using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SmartCarWebApp.Models;
using SmartCarWebApp.Shared;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace SmartCarWebApp.Services;

public class SmartCarService
{
    #region Private Classes
    private class AccessTokenResponse
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; } = default!;

        [JsonProperty("token_type")]
        public string TokenType { get; set; } = default!;

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; } = default!;

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; } = default!;
    }

    private class Tokens
    {
        public string AccessToken { get; set; } = default!;

        public DateTime Expiration { get; set; } = default!;

        public string RefreshToken { get; set; } = default!;

        public Tokens() { }  

        public Tokens(AccessTokenResponse tokenResponse)
        {
            this.AccessToken = tokenResponse.AccessToken;
            this.RefreshToken = tokenResponse.RefreshToken;
            this.Expiration = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        }
    }

    private class HttpResponse
    {
        public string ResponseContent { get; set; } = default!;
        public HttpStatusCode StatusCode { get; set; } = default!;
        public string? ReasonPhrase { get; set; } = default!;

        public bool IsSuccessStatusCode
        {
            get { return ((int)StatusCode >= 200) && ((int)StatusCode <= 299); }
        }
    }
    #endregion

    private string VehiclesUrl = "https://api.smartcar.com/v2.0/vehicles";
    private string ConnectUurl = "https://connect.smartcar.com/oauth/authorize";
    private string TokenUrl = "https://auth.smartcar.com/oauth/token";

    #region Properties
    private HttpClient Client { get; init; } = default!;
    private string ClientID { get; init; } = default!;
    private string ClientSecret { get; init; } = default!;
    private ISession Session { get; init; } = default!;
    private ILogger<SmartCarService> Logger { get; init; } = default!;
    #endregion

    private string User
    {
        // temp workaround until DB is implemented: saving in session

        get
        {
            byte[] data = this.Session.Get("User")!;

            if (data != null)
            {
                return Encoding.UTF8.GetString(data);
            }

            return string.Empty;
        }
        set
        {
            byte[] data = Encoding.UTF8.GetBytes(value);

            this.Session.Set("User", data);
        }
    }

    private ConcurrentDictionary<string, Tokens> TokenStore
    {
        // temp workaround until DB is implemented: saving in session

        get
        {
            byte[] data = this.Session.Get("Tokens")!;

            if (data != null)
            {
                // Deserialize the dictionary
                string json = Encoding.UTF8.GetString(data);
                var store =  JsonConvert.DeserializeObject<ConcurrentDictionary<string, Tokens>>(json);
                if(store != null)
                {
                    return store;
                }
            }

            // If the dictionary doesn't exist in the session, create a new one
            return new ConcurrentDictionary<string, Tokens>();
        }
        set
        {
            // Serialize the dictionary
            string json = JsonConvert.SerializeObject(value);
            byte[] data = Encoding.UTF8.GetBytes(json);

            // Store the serialized dictionary in the session
            this.Session.Set("Tokens", data);
        }
    }

    public SmartCarService(ILogger<SmartCarService> logger, IOptionsSnapshot<Settings> options, 
        HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
    {
        this.Logger = logger;
        if (httpContextAccessor.HttpContext != null)
        {
            this.Session = httpContextAccessor.HttpContext.Session;
        }
        else
        {
            this.Logger.LogCriticalExt("SmartCarService initialzied without an HttpContext.");
        }
        this.Client = httpClient;
        this.ClientID = options.Value.SmartCarClientID;
        this.ClientSecret = options.Value.SmartCarClientSecret;

        this.Logger.LogTraceExt("SmartCarService initialized.");
    }

    /// <summary>
    /// Calls the endpoint to exchange the autorization code for
    /// access and refresh Tokens.
    /// </summary>
    /// <param name="code">Authorization code.</param>
    /// <param name="appCallbackUri">URI to return to upon success.</param>
    /// <exception cref="TokenExchangeException"></exception>
    public async Task TokenExchange(string code, string appCallbackUri)
    {
        this.Logger.LogTraceExt($"TokenExchange method called - code:{code} callback URI: {appCallbackUri}.");

        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "redirect_uri", appCallbackUri }
        };
        
        await this.PerformTokenExchange(code, parameters);
    }

    /// <summary>
    /// Calls the endpoint to retrieve the list of vehicles.
    /// </summary>
    /// <returns>The list of vehicles.</returns>
    /// <exception cref="VehicleListException"></exception>
    public async Task<VehicleList> GetVehicleList()
    {
        this.Logger.LogTraceExt("GetVehicleList method called.");

        var response = await this.GetOrPostVehiclesAsync("");

        if (response.IsSuccessStatusCode)
        {
            var responseObject = JsonConvert.DeserializeObject<VehicleList>(response.ResponseContent);
            if (responseObject != null)
            {
                return responseObject;
            }

            var errorMessage = 
                $"GetVehicleList Error - unable to deserialize reponse into VehicleList object. Response content: '{response.ResponseContent}'";
            this.Logger.LogErrorExt(errorMessage);
            throw new VehicleListException(errorMessage);
        }
        else
        {
            var errorMessage = 
                $"GetVehicleList Error: {response.StatusCode} - {response.ReasonPhrase} content: '{response.ResponseContent}'";
            this.Logger.LogErrorExt(errorMessage);
            throw new VehicleListException(errorMessage);
        }
    }

    /// <summary>
    /// Calls the endpoint to retrieve the info for a vehicle.
    /// </summary>
    /// <param name="id">Vehicle ID.</param>
    /// <returns>VehicleInfo object.</returns>
    /// <exception cref="VehicleInfoException"></exception>
    public async Task<VehicleInfo> GetVehicleInfo(string id)
    {
        this.Logger.LogTraceExt($"GetVehicleInfo method called - id: {id}.");

        var response = await this.GetOrPostVehiclesAsync("/" + id);

        if (response.IsSuccessStatusCode)
        {
            var responseObject = JsonConvert.DeserializeObject<VehicleInfo>(response.ResponseContent);
            if (responseObject != null)
            {
                return responseObject;
            }

            var errorMessage =
                $"GetVehicleInfo Error - unable to deserialize reponse into VehicleInfo object. Response content: '{response.ResponseContent}'";
            this.Logger.LogErrorExt(errorMessage);
            throw new VehicleInfoException(errorMessage);
        }
        else
        {
            var errorMessage =
                $"GetVehicleInfo Error: {response.StatusCode} - {response.ReasonPhrase} content: '{response.ResponseContent}'";
            this.Logger.LogErrorExt(errorMessage);
            throw new VehicleInfoException(errorMessage);
        }
    }

    /// <summary>
    /// Calls the endpoint to lock or unlock the vehicle.
    /// </summary>
    /// <param name="id">Vehicle ID.</param>
    /// <param name="lockVehicle">True to lock vehicle, false otherwise.</param>
    /// <exception cref="LockOrUnlockException"></exception>
    public async Task LockOrUnlock(string id, bool lockVehicle)
    {
        this.Logger.LogTraceExt($"LockOrUnlock method called - id: {id} lockVehichle {lockVehicle}.");

        string lockOrUnlock = (lockVehicle == true) ? "LOCK" : "UNLOCK";
        string jsonData = "{\"action\": \"" + lockOrUnlock + "\"}";

        var response = await this.GetOrPostVehiclesAsync("/" + id + "/security", jsonData);

        if (response.IsSuccessStatusCode)
        {
            this.Logger.LogInformationExt(
                $"Response JSON from Lock/Unlock attempt: {response.ResponseContent}");
        }
        else
        {
            var errorMessage =
                $"LockOrUnlock Error: {response.StatusCode} - {response.ReasonPhrase} content: '{response.ResponseContent}'";
            this.Logger.LogErrorExt(errorMessage);
            throw new LockOrUnlockException(errorMessage);
        }
    }

    /// <summary>
    /// Calls the endpoint to retrieve the security status for a vehicle.
    /// </summary>
    /// <param name="id">Vehicle ID<./param>
    /// <returns>LockStatus object.</returns>
    /// <exception cref="GetLockStatusException"></exception>
    public async Task<LockStatus> GetLockStatus(string id)
    {
        this.Logger.LogTraceExt($"GetLockStatus method called - id: {id}.");

        var response = await this.GetOrPostVehiclesAsync("/" + id + "/security");

        if (response.IsSuccessStatusCode)
        {
            var responseObject = JsonConvert.DeserializeObject<LockStatus>(response.ResponseContent);
            if (responseObject != null)
            {
                return responseObject;
            }

            var errorMessage =
                $"GetLockStatus Error - unable to deserialize reponse into LockStatus object. Response content: '{response.ResponseContent}'";
            this.Logger.LogErrorExt(errorMessage);
            throw new GetLockStatusException(errorMessage);
        }
        else
        {
            var errorMessage =
                $"GetLockStatus Error: {response.StatusCode} - {response.ReasonPhrase} content: '{response.ResponseContent}'";
            this.Logger.LogErrorExt(errorMessage);
            throw new GetLockStatusException(errorMessage);
        }
    }

    /// <summary>
    /// Creates the URL for the SmartCar API's Connect flow.
    /// </summary>
    /// <param name="testMode">True for test mode, false for live mode.</param>
    /// <param name="appCallbackUri">The URI to be called upon success.</param>
    /// <returns>The URL for redirection.</returns>
    public string GetConnectUrl(bool testMode, string appCallbackUri)
    {
        string mode = (testMode == true) ? "test" : "false";
        string smartCarCOnnectUurl = this.ConnectUurl;

        // note to self - can test error handling by changing scope to cause FORBIDDEN returns from api calls
        string scope = "read_vehicle_info read_security control_security";

        string url = $"{ smartCarCOnnectUurl}?response_type=code&client_id={this.ClientID}" +
            $"&scope={scope}&redirect_uri={appCallbackUri}&mode={mode}";

        // separating these out to make changes easier for test purposes
        //url += "&single_select=true";
        url += "&remember_creds=false";

        this.Logger.LogTraceExt($"GetConnectUrl is {url}.");

        return url;
    }

    #region Helper Methods
    /// <summary>
    /// Retrieves the access token for the current user, and refreshes both access and refresh tokens
    /// if within 10 minutes of expiration.
    /// </summary>
    /// <returns>The access token string.</returns>
    private async Task<string> GetAccessToken()
    {
        // temp workaround until DB is implemented: saving in session
        var now = DateTime.UtcNow;

        var tokenStore = this.TokenStore;
        var ret = tokenStore[this.User];

        this.Logger.LogTraceExt(
            $"GetAccessToken() - for user {this.User} found token {ret.AccessToken}");
        this.Logger.LogTraceExt(
            $"GetAccessToken() - for user {this.User} token expires {ret.Expiration} (UTC Now is {DateTime.UtcNow})");
        
        // check if token should be renewed
        if (ret.Expiration.CompareTo(DateTime.UtcNow.AddMinutes(10)) < 0)
        {
            this.Logger.LogInformationExt("Refreshing tokens.");
            return await this.GetNewTokens(this.User, ret.RefreshToken);
        }

        return ret.AccessToken;
    }

    /// <summary>
    /// Helper method to call Get or Post using this.VehicleUrl as base URL.
    /// </summary>
    /// <param name="urlAppendage">Possible add-on to end of URL (may be empty).</param>
    /// <param name="jsonString">If non-null, Post call is made after converting
    /// the JSON string to StringContent.</param>
    /// <returns>Internally-defined class with response information.</returns>
    private async Task<HttpResponse> GetOrPostVehiclesAsync(string urlAppendage, string jsonString = "")
    {
        HttpResponseMessage response;
        string url = this.VehiclesUrl + urlAppendage;

        string accessToken = await this.GetAccessToken();

        this.Logger.LogTraceExt($"GetOrPostVehiclesAsync url is {url}.");

        this.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            if (string.IsNullOrEmpty(jsonString))
            {
                response = await this.Client.GetAsync(url);
            }
            else
            {
                var contentData = new StringContent(jsonString, Encoding.UTF8, "application/json");
                response = await this.Client.PostAsync(url, contentData);
            }
        }
        catch (Exception ex)
        {
            this.Logger.LogCriticalExt(ex, "Http exception caught in GetOrPostVehiclesAsync.");
            throw;
        }

        string responseContent = await response.Content.ReadAsStringAsync();

        return new HttpResponse
        {
            ResponseContent = responseContent,
            StatusCode = response.StatusCode,
            ReasonPhrase = response.ReasonPhrase
        };
    }

    /// <summary>
    /// Calls the endpoint to exchange the current refresh token
    /// for new access and refresh tokens. Both are stored.
    /// </summary>
    /// <param name="user">Current user.</param>
    /// <param name="refreshToken">Refresh token to use in exchange.</param>
    /// <returns>New access token.</returns>
    private async Task<string> GetNewTokens(string user, string refreshToken)
    {
        this.Logger.LogTraceExt($"GetNewTokens method called - user: {user} refresh token: {refreshToken}.");

        var parameters = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", refreshToken }
        };

        return await this.PerformTokenExchange(user, parameters);
    }

    /// <summary>
    /// Calls the endpoint to either:
    /// 1) exchange the current refresh token for new access and refresh tokens, or
    /// 2) exchange the autorization code for first-time access and refresh Tokens.
    /// Both tokens are stored after retrieval.
    /// </summary>
    /// <param name="user">Current user.</param>
    /// <param name="parameters">Data to send as Post content (auth code or refresh token,
    /// and corresponding values as needed for each option).</param>
    /// <returns>New access token.</returns>
    /// <exception cref="TokenExchangeException"></exception>
    private async Task<string> PerformTokenExchange(string user, Dictionary<string, string> parameters)
    {
        this.Logger.LogTraceExt($"GetNewTokens method called - user: {user} parameters: {parameters}.");

        string authorizationHeaderValue =
            Convert.ToBase64String(Encoding.ASCII.GetBytes($"{this.ClientID}:{this.ClientSecret}"));

        string tokenUrl = this.TokenUrl;

        var content = new FormUrlEncodedContent(parameters);

        this.Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", authorizationHeaderValue);

        content.Headers.ContentType =
            new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        HttpResponseMessage response = await this.Client.PostAsync(tokenUrl, content);
        string responseContent = await response.Content.ReadAsStringAsync();

        var responseObject = JsonConvert.DeserializeObject<AccessTokenResponse>(responseContent);

        if (response.IsSuccessStatusCode)
        {
            if (responseObject != null)
            {
                this.User = user;
                var tokens = new Tokens(responseObject);
                var tokenStore = this.TokenStore;
                tokenStore[this.User] = tokens;
                this.TokenStore = tokenStore;

                this.Logger.LogTraceExt(
                    $"PerformTokenExchange - parsed values are AT: {responseObject.AccessToken} RT: {responseObject.RefreshToken}");

                return tokens.AccessToken;
            }
            else
            {
                var errorMessage = "Token Exchange Error - unexpected format.";
                this.Logger.LogCriticalExt(errorMessage);
                throw new TokenExchangeException(errorMessage);
            }
        }
        else
        {
            var errorMessage =
                $"Token Exchange Error: {response.StatusCode} - {response.ReasonPhrase} content: '{responseContent}'.";
            this.Logger.LogCriticalExt(errorMessage);
            throw new TokenExchangeException(errorMessage);
        }
    }
    #endregion
}
