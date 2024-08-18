using GDJ.Auth;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;

var clientId = Environment.GetEnvironmentVariable("GDJ_SPOTIFY_CLIENT_ID");

GDJAuthenticator auth = new GDJAuthenticator(clientId);
//var accessToken = await auth.GetAuthorizationToken();

var spotifyClient = await auth.GetSpotifyClientAsync();

var me = await spotifyClient.UserProfile.Current();
Console.WriteLine($"Hello, {me.DisplayName}");

var result = await spotifyClient.Playlists.GetUsers(me.Id);
Console.WriteLine(result.Total);

