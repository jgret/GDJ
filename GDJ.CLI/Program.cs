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
        service.FetchLibraryAsync();

        while (true)
        {
            await Task.Delay(1000);
        }
    }

    public static async Task printWelcomeMessage(SpotifyClient client)
    {
        var me = await client.UserProfile.Current();
        Console.WriteLine($"Hello, {me.DisplayName}");

        var result = await client.Playlists.CurrentUsers();
        Console.WriteLine($"You have {result.Total} Playlists");
    }
}