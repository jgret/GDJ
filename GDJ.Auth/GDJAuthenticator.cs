using Newtonsoft.Json;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using System.ComponentModel;
using static SpotifyAPI.Web.Scopes;

namespace GDJ.Auth
{
    public class GDJAuthenticator
    {

        private string clientId;
        private string credentialsPath;

        private EmbedIOAuthServer server;

        public GDJAuthenticator(string clientId, string credentialsPath = "credentials.json")
        {
            this.clientId = clientId;
            this.credentialsPath = credentialsPath;

            server = new EmbedIOAuthServer(GetRedirectUrl(), 5543);
        }

        public async Task<SpotifyClient> GetSpotifyClientAsync()
        {

            PKCEAuthenticator? authenticator;

            if (!File.Exists(credentialsPath))
            {
                var result = new TaskCompletionSource<PKCEAuthenticator>();
                var (verifier, challenge) = PKCEUtil.GenerateCodes();

                await server.Start();
                server.AuthorizationCodeReceived += async (sender, response) =>
                {
                    await server.Stop();
                    var token = await new OAuthClient().RequestToken(
                      new PKCETokenRequest(clientId!, response.Code, server.BaseUri, verifier)
                    );

                    await File.WriteAllTextAsync(credentialsPath, JsonConvert.SerializeObject(token));

                    var auth = new PKCEAuthenticator(clientId, token);
                    result.SetResult(auth);
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
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to open URL, manually open: {0}", uri);
                }

                authenticator = await result.Task;
            } 
            else
            {
                var json = await File.ReadAllTextAsync(credentialsPath);
                var token = JsonConvert.DeserializeObject<PKCETokenResponse>(json);

                authenticator = new PKCEAuthenticator(clientId!, token!);
            }

            authenticator.TokenRefreshed += (sender, token) => File.WriteAllText(credentialsPath, JsonConvert.SerializeObject(token));
            var config = SpotifyClientConfig.CreateDefault()
              .WithAuthenticator(authenticator);

            server.Dispose();

            return new SpotifyClient(config);
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
