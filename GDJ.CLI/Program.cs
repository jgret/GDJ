using GDJ.Auth;
using GDJ.Service;
using SpotifyAPI.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var clientId = Environment.GetEnvironmentVariable("GDJ_SPOTIFY_CLIENT_ID");
        GDJAuthenticator auth = new GDJAuthenticator(clientId!);
        var client = await auth.GetSpotifyClientAsync();

        await printWelcomeMessage(client);

        var service = new GDJService(client);
        var lib = await service.RefetchLibraryAsync();

        // set MixRatio for each playlist and return the active playlists
        // Substituted with test functions
        TestFilterPlaylists(lib); // simulates en/disabling playlists
        TestAssignMixRatios(lib); // simulates assigning slidervalues to mixratios

        service.UpdatePlaylists(lib);

        while (true)
        {
            await Task.Delay(1000);
        }
    }

    public static void TestAssignMixRatios(List<PlaylistMix> pl)
    {
        int maxRand = 1000000;
        int rand;

        foreach (var item in pl)
        {
            rand = new Random().Next(0, maxRand);
            item.MixRatio = rand / 1000000.0;
            maxRand -= rand;
        }
    }

    public static List<PlaylistMix> TestPlaylists()
    {
        return
        [
            new("5faTa2QyuNYFBMUD5IqGjL", 0.60),  // DnB Playlist from Smino
            new("3mJgvnYuHwzbCaBue4a47r", 0.30),  // Electronic Playlist from Smino
            new("00DG0aSn5EXOvpLhQxGxzc", 0.10),  // House Playlist from Smino
        ];
    }

    public static void TestFilterPlaylists(List<PlaylistMix> pl)
    {
        pl.RemoveRange(0, 5); // Remove first 5
    }

    public static async Task printWelcomeMessage(SpotifyClient client)
    {
        var me = await client.UserProfile.Current();
        Console.WriteLine($"Hello, {me.DisplayName}");

        var result = await client.Playlists.CurrentUsers();
        Console.WriteLine($"You have {result.Total} PLaylists");
    }
}