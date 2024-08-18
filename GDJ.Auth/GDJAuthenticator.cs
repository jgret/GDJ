using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using static SpotifyAPI.Web.Scopes;

namespace GDJ.Auth
{
    public class GDJAuthenticator
    {

        private string clientId;
        private string accessToken;
        private string credentialsPath;
        private SemaphoreSlim semaphore;

        private EmbedIOAuthServer server;
        private bool serverStarded;

        public GDJAuthenticator(string clientId)
        {
            this.clientId = clientId;
            this.credentialsPath = "credentials.json";
            this.serverStarded = false;
            this.semaphore = new SemaphoreSlim(0, 1);

            server = new EmbedIOAuthServer(GetRedirectUrl(), 5543);
        }

        public async Task<string> GetAuthorizationToken()
        {
            if (!serverStarded)
                await server.Start();

            if (File.Exists(credentialsPath))
            {
                await Start();
            } 
            else
            {
                await StartAuthentication();
            }

            return accessToken;
        }

        public async Task<SpotifyClient> GetSpotifyClientAsync()
        {
            if (!serverStarded)
                await server.Start();

            if (File.Exists(credentialsPath))
            {
                await Start();
            }
            else
            {
                await StartAuthentication();
            }

            return new SpotifyClient(accessToken);
        }

        private async Task Start()
        {
            var json = await File.ReadAllTextAsync(credentialsPath);
            var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

            var authenticator = new PKCEAuthenticator(clientId!, token!);
            authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(credentialsPath, JsonConvert.SerializeObject(token));

            var config = SpotifyClientConfig.CreateDefault()
              .WithAuthenticator(authenticator);

            accessToken = token.AccessToken;

            var spotify = new SpotifyClient(config);

            var me = await spotify.UserProfile.Current();
            Console.WriteLine($"Welcome {me.DisplayName} ({me.Id}), you're authenticated!");

            var playlists = await spotify.PaginateAll(await spotify.Playlists.CurrentUsers().ConfigureAwait(false));
            Console.WriteLine($"Total Playlists in your Account: {playlists.Count}");

            if (semaphore.CurrentCount == 0)
                semaphore.Release();

            server.Dispose();
        }

        private async Task StartAuthentication()
        {
            var (verifier, challenge) = PKCEUtil.GenerateCodes();

            await server.Start();
            server.AuthorizationCodeReceived += async (sender, response) =>
            {
                await server.Stop();
                var token = await new OAuthClient().RequestToken(
                  new PKCETokenRequest(clientId!, response.Code, server.BaseUri, verifier)
                );

                await File.WriteAllTextAsync(credentialsPath, JsonConvert.SerializeObject(token));
                await Start();
            };

            var request = new LoginRequest(server.BaseUri, clientId!, LoginRequest.ResponseType.Code)
            {
                CodeChallenge = challenge,
                CodeChallengeMethod = "S256",
                Scope = new List<string> { UserReadEmail, UserReadPrivate, UserReadPlaybackState, PlaylistReadPrivate, PlaylistReadCollaborative }
            };

            var uri = request.ToUri();
            try
            {
                BrowserUtil.Open(uri);
                await semaphore.WaitAsync();
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to open URL, manually open: {0}", uri);
            }
        }

        private async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await server.Stop();
        }

        public Uri GetRedirectUrl()
        {
            return new Uri("http://localhost:5543/callback");
        }
    }
}
