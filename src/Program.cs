using MonitorProfiles;

if (args.Length == 0)
{
    return InteractiveMenu();
}

switch (args[0].ToLowerInvariant())
{
    case "help":
    case "-h":
    case "--help":
        PrintUsage();
        break;

    case "list":
    case "--list":
        ProfileManager.ListProfiles();
        break;

    case "save":
    case "-s":
    case "--save":
        if (args.Length < 2) { Console.WriteLine("Usage: MonitorProfiles save <name>"); return 1; }
        ProfileManager.SaveProfile(args[1]);
        break;

    case "load":
    case "-l":
    case "--load":
        if (args.Length < 2) { Console.WriteLine("Usage: MonitorProfiles load <name>"); return 1; }
        ProfileManager.LoadProfile(args[1]);
        break;
    
    case "delete":
    case "-d":
    case "--delete":
        if (args.Length < 2) { Console.WriteLine("Usage: MonitorProfiles delete <name>"); return 1; }
        ProfileManager.DeleteProfile(args[1]);
        break;

    default:
        Console.WriteLine($"Unknown command: {args[0]}");
        PrintUsage();
        return 1;
}

return 0;

static int InteractiveMenu()
{
    var profiles = ProfileManager.GetProfileNames();

    Console.WriteLine("MonitorProfiles - Display Setup Profile Manager");
    Console.WriteLine();

    if (profiles.Count == 0)
    {
        Console.WriteLine("No saved profiles.");
        return 0;
    }

    Console.WriteLine("Select a profile to load:");
    Console.WriteLine();
    for (int i = 0; i < profiles.Count; i++)
        Console.WriteLine($"  {i + 1}. {profiles[i]}");
    Console.WriteLine($"  {profiles.Count + 1}. Exit");
    Console.WriteLine();
    Console.Write("> ");

    var input = Console.ReadLine()?.Trim();
    if (!int.TryParse(input, out int num) || num < 1 || num > profiles.Count + 1)
    {
        Console.WriteLine("Invalid selection.");
        return 1;
    }

    if (num == profiles.Count + 1)
        return 0;

    ProfileManager.LoadProfile(profiles[num - 1]);
    return 0;
}

static void PrintUsage()
{
    Console.WriteLine("MonitorProfiles - Display Setup Profile Manager");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  MonitorProfiles                        Interactive mode");
    Console.WriteLine("  MonitorProfiles list|--list            List saved profiles");
    Console.WriteLine("  MonitorProfiles save|-s|--save <name>  Save current display arrangement");
    Console.WriteLine("  MonitorProfiles load|-l|--load <name>  Apply a saved profile");
    Console.WriteLine("  MonitorProfiles delete|-d|--delete <name>  Delete a saved profile");
    Console.WriteLine("  MonitorProfiles help|-h|--help         Show this help message");
}
