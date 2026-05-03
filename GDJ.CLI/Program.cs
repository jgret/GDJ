using GDJ.Auth;
using GDJ.Service;
using SpotifyAPI.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        string? clientId = Environment.GetEnvironmentVariable("GDJ_SPOTIFY_CLIENT_ID");

        if (clientId == null)
            throw new ArgumentException("Please provide your client id so we can log you in to your account");

        GDJAuthenticator auth = new GDJAuthenticator(clientId!);
        SpotifyClient client = await auth.GetSpotifyClientAsync();

        await printWelcomeMessage(client);

        GDJService service = new GDJService(client);
        List<Mix> lib = await service.RefetchLibraryAsync();

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

    public static void TestAssignMixRatios(List<Mix> pl)
    {
        int maxRand = 1000000; // Precision
        int rand;

        foreach (var item in pl)
        {
            rand = new Random().Next(0, maxRand);
            item.MixRatio = rand / 1000000.0;
            maxRand -= rand;
        }
    }

    public static List<Mix> TestPlaylists()
    {
        return
        [
            new("5faTa2QyuNYFBMUD5IqGjL", 0.60),  // DnB Playlist from Smino
            new("3mJgvnYuHwzbCaBue4a47r", 0.30),  // Electronic Playlist from Smino
            new("00DG0aSn5EXOvpLhQxGxzc", 0.10),  // House Playlist from Smino
        ];
    }

    public static void TestFilterPlaylists(List<Mix> pl)
    {
        pl.RemoveRange(0, 5); // Remove first 5 (largest Playlists in Sminos Library)
    }

    public static async Task printWelcomeMessage(SpotifyClient client)
    {
        var me = await client.UserProfile.Current();
        Console.WriteLine($"Hello, {me.DisplayName}");

        var result = await client.Playlists.CurrentUsers();
        Console.WriteLine($"You have {result.Total} PLaylists");
    }
}