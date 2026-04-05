using System.Text.Json;
using Spectre.Console;

string folder = Path.Combine(Directory.GetCurrentDirectory(), "journal-data");
Directory.CreateDirectory(folder);

JournalFiles files = new JournalFiles(folder);
Progress progress = new Progress(files);

UserProfile profile = files.LoadUserProfile();

if (string.IsNullOrWhiteSpace(profile.Name))
{
    AnsiConsole.Clear();
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("[bold]Reflection Journal[/]");
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Let's set up your username.");
    AnsiConsole.WriteLine();

    profile.Name = AnsiConsole.Ask<string>("Username:").Trim();
    files.SaveUserProfile(profile);

    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Username saved: " + profile.Name);
    AnsiConsole.WriteLine();
    AnsiConsole.MarkupLine("Hit Enter to continue.");
    Console.ReadLine();
}

new JournalApp(files, progress).Run(profile);

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine("Goodbye.");

class JournalApp
{
    private readonly JournalFiles files;
    private readonly Progress progress;
    private UserProfile profile = new UserProfile();
    private string today = "";
    private JournalEntry entry = new JournalEntry();

    public JournalApp(JournalFiles files, Progress progress)
    {
        this.files = files;
        this.progress = progress;
    }
    public void Run(UserProfile profile)
    {
        this.profile = profile;
        today = DateTime.Today.ToString("yyyy-MM-dd");
        entry = files.LoadJournalEntry(today);

        while (true)
        {
            AnsiConsole.Clear();
            AnsiConsole.MarkupLine("[bold]" + profile.Name + "'s Journal[/] [dim] Today is " + today + "[/]");
            AnsiConsole.WriteLine();

            bool morningDone = entry.HasMorningReflection();
            bool winToday = entry.HasAchievements();

            AnsiConsole.MarkupLine(morningDone
                ? "Nice! You completed your morning reflection."
                : "Complete your morning reflection.");
            string morningChoice = morningDone ? "Morning Reflection (done!)" : "Morning Reflection";

            AnsiConsole.MarkupLine(winToday
                ? "You logged at least one achievement or small win today."
                : "Log an achievement or small win when you can.");
            string achievementChoice = winToday ? "Achievement or Small Win (logged today)" : "Achievement or Small Win";

            AnsiConsole.WriteLine();

            string choice = Pick(
                "Choose a menu option:",
                new[]
                {
                    morningChoice,
                    achievementChoice,
                    "Progress & Old Entries",
                    "Edit Today's Entry",
                    "Exit"
                });

            AnsiConsole.Clear();

            if (choice == morningChoice)
            {
                if (morningDone)
                {
                    AnsiConsole.MarkupLine("[bold]Morning Reflection[/]");
                    AnsiConsole.WriteLine();
                    AnsiConsole.MarkupLine("What went well yesterday: " + entry.MorningReflection);
                    Pause();
                }
                else
                    MorningReflection();
            }
            else if (choice == achievementChoice)
                RecordAchievement();
            else if (choice == "Progress & Old Entries")
                ShowOldEntries();
            else if (choice == "Edit Today's Entry")
                EditEntry();
            else if (choice == "Exit")
                break;
        }
    }
    private string Pick(string title, string[] choices)
    {
        SelectionPrompt<string> prompt = new SelectionPrompt<string>().Title(title).AddChoices(choices);
        return AnsiConsole.Prompt(prompt);
    }
    private string Pick(string title, string[] choices, int pageSize)
    {
        SelectionPrompt<string> prompt = new SelectionPrompt<string>().Title(title).AddChoices(choices);
        prompt.PageSize(pageSize);
        return AnsiConsole.Prompt(prompt);
    }
    private void Pause()
    {
        AnsiConsole.WriteLine();
        Ui.WaitForEnter();
    }
    private void Pause(string message)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(message);
        Ui.WaitForEnter();
    }
    private void ClearScreen(string boldTitle)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold]" + boldTitle + "[/]");
        AnsiConsole.WriteLine();
    }
    private void ClearScreen(string boldTitle, string dimSuffix)
    {
        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold]" + boldTitle + "[/] [dim]" + dimSuffix + "[/]");
        AnsiConsole.WriteLine();
    }
    private static string AskTrimmed(string label)
    {
        return AnsiConsole.Prompt(new TextPrompt<string>(label).AllowEmpty()).Trim();
    }
    private void SaveTodayAndPause(string message = "Saved.")
    {
        files.SaveJournalEntry(entry);
        Pause(message);
    }
    private void WriteAchievements(List<string> achievements, string emptyWhenNone, bool blankLineAfterBullets)
    {
        if (achievements.Count == 0)
        {
            if (emptyWhenNone != "")
                AnsiConsole.MarkupLine(emptyWhenNone);
            return;
        }
        foreach (string win in achievements)
            AnsiConsole.MarkupLine("- " + win);

        if (blankLineAfterBullets)
            AnsiConsole.WriteLine();
    }
    private void MorningReflection()
    {
        ClearScreen("Morning Reflection");

        entry.MorningReflection = AnsiConsole.Ask<string>("What went well yesterday:");
        SaveTodayAndPause();
    }
    private void RecordAchievement()
    {
        ClearScreen("Achievement or Small Win");

        WriteAchievements(entry.Achievements, "", true);

        string subChoice = Pick("Choose:", new[] { "Add an achievement or small win", "Back to main menu" });

        if (subChoice == "Back to main menu")
            return;

        string prompt = entry.HasAchievements()
            ? "Add another achievement or small win for today:"
            : "What is one achievement or small win for today?";

        string winText = AnsiConsole.Ask<string>(prompt).Trim();

        if (string.IsNullOrWhiteSpace(winText))
        {
            Pause("Nothing saved—achievement was blank.");
            return;
        }

        entry.AddAchievement(winText);
        SaveTodayAndPause();
    }
    private void EditMorningFromMenu()
    {
        ClearScreen("Edit morning reflection");

        if (!entry.HasMorningReflection())
            AnsiConsole.MarkupLine("No morning reflection yet.");
        else
            AnsiConsole.MarkupLine("Current: " + entry.MorningReflection);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Leave blank to keep it. Type delete to erase it.");

        string newText = AskTrimmed("New entry:");

        if (newText == "delete")
            entry.MorningReflection = "";
        else if (newText != "")
            entry.MorningReflection = newText;

        SaveTodayAndPause();
    }
    private void EditAchievementsMenu()
    {
        ClearScreen("Manage achievements");

        if (!entry.HasAchievements())
        {
            AnsiConsole.MarkupLine("No achievements to manage yet.");
            Pause();
            return;
        }

        List<string> pickChoices = new List<string>();
        for (int i = 0; i < entry.Achievements.Count; i++)
            pickChoices.Add((i + 1) + ". " + entry.Achievements[i]);

        pickChoices.Add("Cancel");

        string picked = Pick("Pick an achievement:", pickChoices.ToArray());

        if (picked == "Cancel")
            return;

        int idx = JournalEntry.GetAchievementIndex(picked, entry.Achievements.Count);
        if (idx == -1)
        {
            Pause("Could not figure out which achievement you picked.");
            return;
        }

        string action = Pick("What do you want to do?", new[] { "Edit", "Delete", "Cancel" });

        if (action == "Cancel")
            return;

        if (action == "Delete")
        {
            entry.Achievements.RemoveAt(idx);
            SaveTodayAndPause("Deleted.");
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Current: " + entry.Achievements[idx]);
        AnsiConsole.WriteLine();

        string editNewText = AskTrimmed("New text:");

        if (string.IsNullOrWhiteSpace(editNewText))
        {
            Pause("Nothing saved—that was blank.");
            return;
        }

        entry.Achievements[idx] = editNewText;
        SaveTodayAndPause();
    }
    private void EditEntry()
    {
        while (true)
        {
            ClearScreen("Edit today's entry", today);
            AnsiConsole.MarkupLine("Change or remove something you already wrote today.");
            AnsiConsole.MarkupLine("[dim]To log a new win, pick Achievement or Small Win on the main menu.[/]");
            AnsiConsole.WriteLine();

            string editChoice = Pick(
                "Edit menu:",
                new[] { "Edit morning reflection", "Manage achievements", "Back to main menu" });

            if (editChoice == "Back to main menu")
                break;

            if (editChoice == "Edit morning reflection")
                EditMorningFromMenu();
            else
                EditAchievementsMenu();
        }
    }
    private void ShowPastDay(string isoDate)
    {
        ClearScreen("Past entry", isoDate);

        JournalEntry oldEntry = files.LoadJournalEntry(isoDate);

        AnsiConsole.MarkupLine("[bold]Morning reflection[/]");
        if (!oldEntry.HasMorningReflection())
            AnsiConsole.MarkupLine("(none for this day)");
        else
            AnsiConsole.MarkupLine(oldEntry.MorningReflection);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Achievements & small wins[/]");
        WriteAchievements(oldEntry.Achievements, "(none for this day)", false);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Moments: this journal saves bright spots as achievements and small wins above.[/]");
        AnsiConsole.WriteLine();
    }
    private void PastEntriesMenu()
    {
        while (true)
        {
            ClearScreen("Progress & Old Entries", "View past entries");

            List<string> dates = files.GetJournalDates();

            if (dates.Count == 0)
            {
                AnsiConsole.MarkupLine("No saved journal days yet.");
                Pause();
                return;
            }

            List<string> choices = new List<string>(dates);
            choices.Add("Back to Progress menu");

            string picked = Pick("Pick a day to open:", choices.ToArray(), 15);

            if (picked == "Back to Progress menu")
                return;

            ShowPastDay(picked);
            Pause();
        }
    }
    private void ShowOldEntries()
    {
        while (true)
        {
            ClearScreen("Progress & Old Entries");
            AnsiConsole.MarkupLine("Look back at what you wrote on other days.");
            AnsiConsole.WriteLine();

            string progressChoice = Pick(
                "Choose:",
                new[] { "View past entries", "Pattern summaries", "Return to main menu" });

            if (progressChoice == "Return to main menu")
                return;

            if (progressChoice == "View past entries")
                PastEntriesMenu();
            else if (progressChoice == "Pattern summaries")
                progress.ShowPatternSummaries();
        }
    }
}

static class Ui
{
    public static void WaitForEnter()
    {
        AnsiConsole.MarkupLine("Press Enter to go back.");
        Console.ReadLine();
    }
}

class JournalEntry
{
    public string Date { get; set; } = "";
    public string MorningReflection { get; set; } = "";
    public List<string> Achievements { get; set; } = new List<string>();

    public static int GetAchievementIndex(string choice, int achievementCount)
    {
        int dot = choice.IndexOf('.');
        if (dot <= 0)
            return -1;

        string numberText = choice.Substring(0, dot).Trim();

        if (!int.TryParse(numberText, out int oneBased))
            return -1;

        int index = oneBased - 1;

        if (index < 0 || index >= achievementCount)
            return -1;

        return index;
    }
    public bool HasMorningReflection()
    {
        return !string.IsNullOrWhiteSpace(MorningReflection);
    }
    public bool HasAchievements()
    {
        return Achievements.Count > 0;
    }
    public void AddAchievement(string text)
    {
        text = text.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return;

        Achievements.Add(text);
    }
    public int GetActivityLevel()
    {
        int winCount = Achievements.Count;
        bool hasMorning = HasMorningReflection();

        if (!hasMorning && winCount == 0)
            return 0;
        if (hasMorning && winCount == 0)
            return 1;
        if (!hasMorning && winCount == 1)
            return 1;
        if (hasMorning && winCount == 1)
            return 2;
        if (!hasMorning && winCount == 2)
            return 2;
        return 3;
    }
}

class UserProfile
{
    public string Name { get; set; } = "";
}

class JournalFiles
{
    private static readonly JsonSerializerOptions IndentedJson = new JsonSerializerOptions { WriteIndented = true };
    private string folder;

    public JournalFiles(string folder)
    {
        this.folder = folder;
    }
    public JournalEntry LoadJournalEntry(string date)
    {
        string path = Path.Combine(folder, date + ".json");

        if (!File.Exists(path))
            return new JournalEntry { Date = date };

        string json = File.ReadAllText(path);
        JournalEntry? loadedEntry = JsonSerializer.Deserialize<JournalEntry>(json);

        if (loadedEntry == null)
            loadedEntry = new JournalEntry { Date = date };

        if (loadedEntry.Achievements == null)
            loadedEntry.Achievements = new List<string>();

        return loadedEntry;
    }
    public void SaveJournalEntry(JournalEntry entry)
    {
        string path = Path.Combine(folder, entry.Date + ".json");
        string json = JsonSerializer.Serialize(entry, IndentedJson);
        File.WriteAllText(path, json);
    }
    public List<string> GetJournalDates()
    {
        string[] paths = Directory.GetFiles(folder, "????-??-??.json");
        List<string> dates = new List<string>();

        foreach (string path in paths)
            dates.Add(Path.GetFileNameWithoutExtension(path));

        dates.Sort();
        dates.Reverse();
        return dates;
    }
    public UserProfile LoadUserProfile()
    {
        string path = Path.Combine(folder, "profile.json");

        if (!File.Exists(path))
            return new UserProfile();

        string json = File.ReadAllText(path);
        UserProfile? profileData = JsonSerializer.Deserialize<UserProfile>(json);

        if (profileData == null)
            profileData = new UserProfile();

        return profileData;
    }
    public void SaveUserProfile(UserProfile profile)
    {
        string path = Path.Combine(folder, "profile.json");
        string json = JsonSerializer.Serialize(profile, IndentedJson);
        File.WriteAllText(path, json);
    }
}

class Progress
{
    private readonly JournalFiles files;

    public Progress(JournalFiles files)
    {
        this.files = files;
    }
    private Dictionary<DateTime, int> LevelsByDate()
    {
        Dictionary<DateTime, int> levelByDate = new Dictionary<DateTime, int>();
        foreach (string iso in files.GetJournalDates())
        {
            if (!DateTime.TryParse(iso, out DateTime parsedDate))
                continue;

            JournalEntry loaded = files.LoadJournalEntry(iso);
            int level = loaded.GetActivityLevel();
            levelByDate[parsedDate.Date] = level;
        }

        return levelByDate;
    }
    public string HeatmapCellMarkup(DateTime cellDate, DateTime todayDt, Dictionary<DateTime, int> levelByDate)
    {
        if (cellDate.Date > todayDt)
            return "[grey]■[/]";

        int level = 0;
        if (levelByDate.TryGetValue(cellDate.Date, out int found))
            level = found;

        if (level == 0)
            return "[red]■[/]";
        if (level == 1)
            return "[yellow]■[/]";
        if (level == 2)
            return "[orange1]■[/]";
        return "[green]■[/]";
    }
    public int GetCurrentStreak(Dictionary<DateTime, int> levelByDate, DateTime today)
    {
        int currentStreak = 0;
        for (DateTime d = today.Date; levelByDate.TryGetValue(d.Date, out int lvl) && lvl > 0; d = d.AddDays(-1))
            currentStreak++;

        return currentStreak;
    }
    public int GetLongestStreak(Dictionary<DateTime, int> levelByDate)
    {
        List<DateTime> activeDays = new List<DateTime>();
        foreach (KeyValuePair<DateTime, int> pair in levelByDate)
        {
            if (pair.Value > 0)
                activeDays.Add(pair.Key);
        }

        activeDays.Sort();

        int longestStreak = 0;
        if (activeDays.Count > 0)
        {
            int runLength = 1;
            longestStreak = 1;
            for (int i = 1; i < activeDays.Count; i++)
            {
                if (activeDays[i] == activeDays[i - 1].AddDays(1))
                    runLength++;
                else
                    runLength = 1;

                if (runLength > longestStreak)
                    longestStreak = runLength;
            }
        }

        return longestStreak;
    }
    public void ShowPatternSummaries()
    {
        // GitHub-style week columns use Sunday as the first day of the week.
        DayOfWeek weekStartsOn = DayOfWeek.Sunday;
        int weekCount = 52;

        Dictionary<DateTime, int> levelByDate = LevelsByDate();
        DateTime todayDt = DateTime.Today;

        int currentStreak = GetCurrentStreak(levelByDate, todayDt);
        int longestStreak = GetLongestStreak(levelByDate);

        int offsetToWeekStart = ((int)todayDt.DayOfWeek - (int)weekStartsOn + 7) % 7;
        DateTime startOfThisWeek = todayDt.AddDays(-offsetToWeekStart);
        DateTime gridWeek0Start = startOfThisWeek.AddDays(-(weekCount - 1) * 7);

        string[] dayLabels = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

        AnsiConsole.Clear();
        AnsiConsole.MarkupLine("[bold]Progress & Old Entries[/] [dim]Pattern summaries[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Streaks[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Current streak: [bold]" + currentStreak + "[/] day(s) in a row (counts today if you journaled today).");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Longest streak: [bold]" + longestStreak + "[/] day(s) in a row (using all saved journal days).");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Activity[/] [dim](last 365 days)[/]");
        AnsiConsole.WriteLine();

        for (int row = 0; row < 7; row++)
        {
            string line = "[bold]" + dayLabels[row].PadRight(3) + "[/] ";
            for (int week = 0; week < weekCount; week++)
            {
                DateTime cellDate = gridWeek0Start.AddDays(week * 7 + row);
                line += HeatmapCellMarkup(cellDate, todayDt, levelByDate);
            }

            AnsiConsole.MarkupLine(line);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Legend[/]");
        AnsiConsole.MarkupLine("[red]■[/] no log   [yellow]■[/] low   [orange1]■[/] medium   [green]■[/] high");
        AnsiConsole.WriteLine();
        Ui.WaitForEnter();
    }
}
