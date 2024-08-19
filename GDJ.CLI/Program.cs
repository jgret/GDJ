using GDJ.Auth;
using GDJ.Service;

using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using static System.Net.WebRequestMethods;

var clientId = Environment.GetEnvironmentVariable("GDJ_SPOTIFY_CLIENT_ID");

GDJAuthenticator auth = new GDJAuthenticator(clientId!);
//var accessToken = await auth.GetAuthorizationToken();

var spotifyClient = await auth.GetSpotifyClientAsync();

var me = await spotifyClient.UserProfile.Current();
Console.WriteLine($"Hello, {me.DisplayName}");

var result = await spotifyClient.Playlists.GetUsers(me.Id);
Console.WriteLine(result.Total);

var service = new GDJService(spotifyClient);

await service.InitDevice();

service.UpdatePlaylists(new List<Playlist>
{
    new Playlist("5faTa2QyuNYFBMUD5IqGjL", 0.5),  // DnB Playlist from Smino
    new Playlist("3mJgvnYuHwzbCaBue4a47r", 0.30), // Electronic Playlist from Smino
    new Playlist("00DG0aSn5EXOvpLhQxGxzc", 0.20),  // House Playlist from Smino
});

while (true)
{
    await Task.Delay(1000);
}

