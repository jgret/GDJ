using GDJ.Auth;
using GDJ.Service;

using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using static System.Net.WebRequestMethods;

public enum Commands
{
    Exit,
    DisplayMix,
    DisplayAll,
    SetEnabled,
    SetMixRatio,
    RefreshLibrary,
    Help
}

public class Program {
    
    static var testPlst = new List<PlaylistMix>
    {
        new PlaylistMix("5faTa2QyuNYFBMUD5IqGjL", 0.60),  // DnB Playlist from Smino
        new PlaylistMix("3mJgvnYuHwzbCaBue4a47r", 0.30),  // Electronic Playlist from Smino
        new PlaylistMix("00DG0aSn5EXOvpLhQxGxzc", 0.10),  // House Playlist from Smino
    };

    public static async Task Main(string[] args) // args: --gui --nogui
    {
        var clientId = Environment.GetEnvironmentVariable("GDJ_SPOTIFY_CLIENT_ID");

        GDJAuthenticator auth = new GDJAuthenticator(clientId!);
        var spotifyClient = await auth.GetSpotifyClientAsync();

        var me = await spotifyClient.UserProfile.Current();
        Console.WriteLine($"Hello, {me.DisplayName}");

        var result = await spotifyClient.Playlists.GetUsers(me.Id);
        Console.WriteLine(result.Total);

        var service = new GDJService(spotifyClient);
        var userLib = await service.RefetchLibrary();
        var enabled = new List<PlaylistMix>();

        // service.UpdatePlaylists(testPlrt);   
        
        // -----------------------------------
        // --- Begin Commandline Interface ---
        // -----------------------------------

        Console.WriteLine("Welcome to the GDJ CLI");

        while (true)
        {
            /*
            - exit
            - displayMix (plst name and printSlider)
            - displayAll
            - setEnabled <name> <true/false>
            - setMixRatio (-> selectFromEnabled -> enter mix ratio -> downscaleOthers -> update)
            - refreshLibrary (must be confirmed)
            - help (exit, displayEnabled, displayAll, setEnabled, setMixRatio, refreshLibrary)
            */

            Console.WriteLine("Enter a command:");
            var command = Console.ReadLine();
            var cmdArgs = command.Split(' ');

            switch (cmdArgs[0])
            {
                case "exit":
                    return;
                case "displayMix":
                    enabled.ForEach(p => Console.WriteLine($"{p.Name} : {Slider(p.MixRatio)} : {p.MixRatio}"));
                    break;
                case "displayAll":
                    userLib.ForEach(p => Console.WriteLine($"{p.Name}"));
                    break;
                case "setEnabled":
                    var plst = userLib.Find(p => p.Name == cmdArgs[1]);
                    if (plst == null)
                    {
                        Console.WriteLine("Playlist not found.");
                        break;
                    }
                    enabled.Add(plst);
                    break;
                case "setMixRatio":
                    Console.WriteLine("Select a playlist:");
                    for (int i = 0; i < enabled.Count; i++)
                    {
                        Console.WriteLine($"{i + 1}. {enabled[i].Name}");
                    }
                    try
                    {
                        var index = int.Parse(Console.ReadLine());
                        var plst = enabled[index];
                        Console.WriteLine($"Enter a mix ratio for {plst.Name} (0-1):");
                        var ratio = double.Parse(Console.ReadLine());
                        plst.MixRatio = ratio;
                        enabled.ForEach(p => p.MixRatio = p == plst ? ratio : p.MixRatio);
                        // downscale others
                        enabled.ForEach(p => p.MixRatio = p == plst ? ratio : (p.MixRatio / (enabled.Sum(p => p.MixRatio)-ratio)));
                    }
                    catch (FormatException e)
                    {
                        Console.WriteLine("Invalid number. >:(");
                    }
                    break;
                case "refreshLibrary":
                    Console.WriteLine("Confirm? (y/n)");
                    var confirm = Console.ReadLine();
                    if (confirm == "y")
                    {
                        userLib = await service.RefetchLibrary();
                    }
                    enabled.RemoveAll(p => !userLib.Contains(p));
                    break;
                case "help":
                    Console.WriteLine("Commands:\nexit\ndisplayMix\ndisplayAll\nsetEnabled <name>\nsetEnabled <name>\nsetMixRatio\nrefreshLibrary\nhelp");
                    break;
                default:
                    Console.WriteLine("Invalid command. Type 'help' for a list of commands.");
                    break;
            }
        }
    }

    public static string Slider(double ratio)
    {
        var slider = new StringBuilder();
        for (int i = 0; i < 10; i++)
        {
            if (i < ratio * 10)
            {
                slider.Append("█");
            }
            else
            {
                slider.Append("-");
            }
        }
        return slider.ToString();
    }

    public static void TestDistributionAlgo()
    {
        const int ITERATIONS = 10;
        Dictionary<string, PlaylistMix> dict = testPlst.ToDictionary(p => p.Id, p => p);

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
    }
}