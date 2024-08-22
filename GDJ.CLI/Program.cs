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

var pl = new List<PlaylistMix>
{
    new PlaylistMix("5faTa2QyuNYFBMUD5IqGjL", 0.60),  // DnB Playlist from Smino
    new PlaylistMix("3mJgvnYuHwzbCaBue4a47r", 0.30),  // Electronic Playlist from Smino
    new PlaylistMix("00DG0aSn5EXOvpLhQxGxzc", 0.10),  // House Playlist from Smino
};

var service = new GDJService(spotifyClient);

await service.RefetchLibrary();

service.UpdatePlaylists(pl);    

// Test playlist distribution

const int ITERATIONS = 10;
Dictionary<string, PlaylistMix> dict = pl.ToDictionary(p => p.Id, p => p);

List<string> played = new List<string>();

for (int i = 1; i <= ITERATIONS; i++)
{
    var key = dict
        .OrderByDescending(kvp => (kvp.Value.MixRatio * i) - kvp.Value.NumPlayed)
        .First().Key;
    dict[key].NumPlayed++;

    Console.WriteLine($"{key}");
}

foreach (var p in dict)
{
    Console.WriteLine($"{p.Key} - Soll={p.Value.MixRatio} Ist={(double)p.Value.NumPlayed / ITERATIONS}");
}
