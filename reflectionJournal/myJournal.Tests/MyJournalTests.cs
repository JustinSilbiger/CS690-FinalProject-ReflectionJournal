using Xunit;

namespace myJournal.Tests;

public class FileManagerTests
{
    [Fact]
    public void SaveAndLoadKeepsJournalEntryData()
    {
        string folder = Path.Combine(Path.GetTempPath(), "journal-test-" + Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(folder);

        try
        {
            JournalFiles files = new JournalFiles(folder);
            string date = "2026-03-01";

            JournalEntry entry = new JournalEntry
            {
                Date = date,
                MorningReflection = "Slept okay and the lecture on recursion finally clicked.",
                Achievements = new List<string> { "Turned in the lab a day early." }
            };

            files.SaveJournalEntry(entry);
            JournalEntry loaded = files.LoadJournalEntry(date);

            Assert.Equal(entry.MorningReflection, loaded.MorningReflection);
            Assert.Single(loaded.Achievements);
            Assert.Equal("Turned in the lab a day early.", loaded.Achievements[0]);
        }
        finally
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }
    }
}

public class ProgressTests
{
    [Fact]
    public void EmptyListHasNoStreak()
    {
        JournalFiles files = new JournalFiles(Path.GetTempPath());
        Progress progress = new Progress(files);

        Assert.Equal(0, progress.GetLongestStreak(new Dictionary<DateTime, int>()));
    }
}

public class JournalEntryTests
{
    [Fact]
    public void AddAchievementSkipsBlankLines()
    {
        JournalEntry entry = new JournalEntry();

        entry.AddAchievement("");
        entry.AddAchievement("   ");
        Assert.False(entry.HasAchievements());

        entry.AddAchievement("Studied for an hour at the library");

        Assert.Single(entry.Achievements);
        Assert.Equal("Studied for an hour at the library", entry.Achievements[0]);
    }
}
