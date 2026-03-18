using System.Text.Json;
using Spectre.Console;


string dataFolder = Path.Combine(Directory.GetCurrentDirectory(), "journal-data");
Directory.CreateDirectory(dataFolder);

string today = DateTime.Today.ToString("yyyy-MM-dd");

UserProfile profile = LoadProfile();

if (string.IsNullOrWhiteSpace(profile.Name))
{
    AnsiConsole.Clear();
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Reflection Journal[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Let's set up your username.");
    AnsiConsole.WriteLine();

    profile.Name = AnsiConsole.Ask<string>("Username:").Trim();
    SaveProfile(profile);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Username saved: " + profile.Name);
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Hit Enter to continue.");
    while (Console.ReadKey(true).Key != ConsoleKey.Enter) { }
}

JournalEntry entry = LoadJournalEntry(today);

while (true)
{
    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[bold]" + profile.Name + "'s Journal[/] [dim] Today is " + today + "[/]");
    AnsiConsole.WriteLine();

    bool morningDone = !string.IsNullOrWhiteSpace(entry.MorningReflection);

    if (morningDone)
        AnsiConsole.MarkupLine("Nice! You completed your morning reflection.");
    else
        AnsiConsole.MarkupLine("Complete your morning reflection.");

    AnsiConsole.WriteLine();

    string morningChoice;
    if (morningDone)
        morningChoice = "Morning Reflection (done!)";
    else
        morningChoice = "Morning Reflection";

    string choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Choose a menu option:")
            .AddChoices(
                morningChoice,
                "Progress & Old Entries",
                "Edit Today's Entry",
                "Exit"));

    AnsiConsole.Clear();

    if (choice == morningChoice)
    {
        if (morningDone)
        {
            AnsiConsole.MarkupLine("[bold]Morning Reflection[/]");
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("What went well yesterday: " + entry.MorningReflection);
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Press Enter to go back.");
            Console.ReadLine();
        }
        else
        {
            MorningReflection();
        }
    }
    else if (choice == "Progress & Old Entries")
    {
        ShowOldEntries();
    }
    else if (choice == "Edit Today's Entry")
    {
        EditEntry();
    }
    else if (choice == "Exit")
    {
        break;
    }
}

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("Goodbye.");

JournalEntry LoadJournalEntry(string date)
{
    string path = Path.Combine(dataFolder, date + ".json");

    if (!File.Exists(path))
        return new JournalEntry { Date = date };

    string json = File.ReadAllText(path);
    JournalEntry? loadedEntry = JsonSerializer.Deserialize<JournalEntry>(json);

    if (loadedEntry == null)
        loadedEntry = new JournalEntry { Date = date };

    return loadedEntry;
}

void SaveEntry(JournalEntry currentEntry)
{
    string path = Path.Combine(dataFolder, currentEntry.Date + ".json");
    string json = JsonSerializer.Serialize(currentEntry, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(path, json);
}

UserProfile LoadProfile()
{
    string path = Path.Combine(dataFolder, "profile.json");

    if (!File.Exists(path))
        return new UserProfile();

    string json = File.ReadAllText(path);
    UserProfile? profileData = JsonSerializer.Deserialize<UserProfile>(json);

    if (profileData == null)
        profileData = new UserProfile();

    return profileData;
}

void SaveProfile(UserProfile currentProfile)
{
    string path = Path.Combine(dataFolder, "profile.json");
    string json = JsonSerializer.Serialize(currentProfile, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(path, json);
}

void MorningReflection()
{
    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[bold]Morning Reflection[/]");
    AnsiConsole.WriteLine();

    entry.MorningReflection = AnsiConsole.Ask<string>("What went well yesterday:");
    SaveEntry(entry);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Saved.");
    AnsiConsole.MarkupLine("Press Enter to go back.");
    Console.ReadLine();
}

void EditEntry()
{
    entry = LoadJournalEntry(today);

    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[bold]Edit today's entry[/] [dim]" + today + "[/]");
    AnsiConsole.WriteLine();

    if (string.IsNullOrWhiteSpace(entry.MorningReflection))
        AnsiConsole.MarkupLine("No morning reflection yet.");
    else
        AnsiConsole.MarkupLine("Current: " + entry.MorningReflection);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Leave blank to keep it. Type delete to erase it.");

    string newText = AnsiConsole.Prompt(
        new TextPrompt<string>("New entry:").AllowEmpty());

    newText = newText.Trim();

    if (newText == "delete")
        entry.MorningReflection = "";
    else if (newText != "")
        entry.MorningReflection = newText;

    SaveEntry(entry);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Saved.");
    AnsiConsole.MarkupLine("Press Enter to go back.");
    Console.ReadLine();
}

void ShowOldEntries()
{
    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[bold]Progress & Old Entries[/]");
    AnsiConsole.WriteLine();

    string[] files = Directory.GetFiles(dataFolder, "????-??-??.json");
    List<string> dates = new List<string>();

    foreach (string file in files)
        dates.Add(Path.GetFileNameWithoutExtension(file));

    dates.Sort();
    dates.Reverse();

    if (dates.Count == 0)
    {
        AnsiConsole.MarkupLine("No entries yet.");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Press Enter to go back.");
        Console.ReadLine();
        return;
    }

    dates.Add("Return to main menu");

    string picked = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Select an entry:")
            .AddChoices(dates));

    if (picked == "Return to main menu")
        return;

    AnsiConsole.Clear();
    AnsiConsole.MarkupLine("[bold]Entry[/] [dim]" + picked + "[/]");
    AnsiConsole.WriteLine();

    JournalEntry oldEntry = LoadJournalEntry(picked);

    if (string.IsNullOrWhiteSpace(oldEntry.MorningReflection))
        AnsiConsole.MarkupLine("Morning reflection: (empty)");
    else
        AnsiConsole.MarkupLine("Morning reflection: " + oldEntry.MorningReflection);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Press Enter to go back.");
    Console.ReadLine();
}

class JournalEntry
{
    public string Date { get; set; } = "";
    public string MorningReflection { get; set; } = "";
}

class UserProfile
{
    public string Name { get; set; } = "";
}