using GDJ.Auth;
using GDJ.Service;

var clientId = Environment.GetEnvironmentVariable("GDJ_SPOTIFY_CLIENT_ID");
GDJAuthenticator auth = new GDJAuthenticator(clientId!);
var spotifyClient = await auth.GetSpotifyClientAsync();

var me = await spotifyClient.UserProfile.Current();
Console.WriteLine($"Hello, {me.DisplayName}");

var result = await spotifyClient.Playlists.CurrentUsers();
Console.WriteLine(result.Total);

var pl = new List<PlaylistMix>
{
    new("5faTa2QyuNYFBMUD5IqGjL", 0.60),  // DnB Playlist from Smino
    new("3mJgvnYuHwzbCaBue4a47r", 0.30),  // Electronic Playlist from Smino
    new("00DG0aSn5EXOvpLhQxGxzc", 0.10),  // House Playlist from Smino
};

var service = new GDJService(spotifyClient);
var pTest = await service.RefetchLibrary();

// pTest has all the playlists from the user
// set MixRatio for each playlist and return the active playlists
// --> service.UpdatePlaylists(pTest);

service.UpdatePlaylists(pl);

while (true)
{
    await Task.Delay(1000);
}