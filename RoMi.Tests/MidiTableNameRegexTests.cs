using System.Text.RegularExpressions;

namespace RoMi.Tests;

[TestFixture]
internal class MidiTableNameRegexTests
{
    [Test]
    [TestCase("* [Program]\n")]
    [TestCase("* [Setup]\n")]
    [TestCase("* [PatchFx1SlowGearBass, PatchFx2SlowGearBass, PatchFx3SlowGearBass,\nPatchFx4SlowGearBass(*)]\n")]
    [TestCase("* Program\n")]
    [TestCase("* Temporary Program\n")]
    [TestCase("*1 KNOB SETTING TABLE\n")]
    [TestCase("*2 ASSIGN TARGET TABLE\n")]
    [TestCase(" *1 KNOB SETTING TABLE\n")]
    public void MidiTableNameRegex_ValidTableNames_ShouldMatch(string input)
    {
        // Act
        bool isMatch = GeneratedRegex.MidiTableNameRegex().IsMatch(input);

        // Assert
        Assert.That(isMatch, Is.True, $"Expected '{input}' to match MidiTableNameRegex");
    }

    [Test]
    [TestCase("Not a table name\n")]
    [TestCase("| 00 00 00 | Program Common [Program Common] |\n")]
    [TestCase("Some random text\n")]
    [TestCase("# Comment\n")]
    [TestCase("Table without asterisk\n")]
    [TestCase("")]
    [TestCase("* \n")] // Asterisk with space but no content
    public void MidiTableNameRegex_InvalidTableNames_ShouldNotMatch(string input)
    {
        // Act
        bool isMatch = GeneratedRegex.MidiTableNameRegex().IsMatch(input);

        // Assert
        Assert.That(isMatch, Is.False, $"Expected '{input}' to not match MidiTableNameRegex");
    }

    [Test]
    [TestCase("* [Program]\n* [Setup]\n* [Common]\n", 3)]
    [TestCase("*1 TABLE ONE\n*2 TABLE TWO\n*3 TABLE THREE\n", 3)]
    [TestCase("* [Program]\nSome content\n* [Setup]\nMore content\n", 2)]
    [TestCase(" *1 KNOB SETTING TABLE\n *2 ASSIGN TARGET TABLE\n", 2)]
    public void MidiTableNameRegex_MultipleMatches_ShouldFindAll(string input, int expectedMatchCount)
    {
        // Act
        var matches = GeneratedRegex.MidiTableNameRegex().Matches(input);

        // Assert
        Assert.That(matches.Count, Is.EqualTo(expectedMatchCount),
            $"Expected {expectedMatchCount} matches in '{input}', but found {matches.Count}");
    }

    [Test]
    public void MidiTableNameRegex_SplitFunctionality_ShouldSplitCorrectly()
    {
        // Arrange
        string input = "Initial content\n* [Program]\nProgram content\n* [Setup]\nSetup content\n";

        // Act
        string[] splits = GeneratedRegex.MidiTableNameRegex().Split(input);

        // Assert
        Assert.That(splits.Length, Is.EqualTo(3), "Expected 3 splits");
        Assert.That(splits[0], Is.EqualTo("Initial content\n"));
        Assert.That(splits[1], Does.Contain("Program content"));
        Assert.That(splits[2], Does.Contain("Setup content"));
    }

    [Test]
    [TestCase("* [Program]\n", "AX-Edge format with brackets")]
    [TestCase("* Program\n", "RD-2000 format without brackets")]
    [TestCase("*1 KNOB SETTING TABLE\n", "GT-1000 numbered format")]
    [TestCase(" *1 KNOB SETTING TABLE\n", "GT-1000 numbered format with leading space")]
    public void MidiTableNameRegex_DifferentFormats_ShouldAllMatch(string input, string description)
    {
        // Act
        bool isMatch = GeneratedRegex.MidiTableNameRegex().IsMatch(input);

        // Assert
        Assert.That(isMatch, Is.True, $"Expected {description} to match: '{input}'");
    }

    [Test]
    public void MidiTableNameRegex_MultilineTableName_ShouldMatch()
    {
        // Arrange
        string input = "* [PatchFx1SlowGearBass, PatchFx2SlowGearBass, PatchFx3SlowGearBass,\nPatchFx4SlowGearBass(*)]\n";

        // Act
        bool isMatch = GeneratedRegex.MidiTableNameRegex().IsMatch(input);

        // Assert
        Assert.That(isMatch, Is.True, "Multiline table names should be matched");
    }

    [Test]
    [TestCase("* [Program]\nContent\n* [Setup]\nMore content\n")]
    public void MidiTableNameRegex_UsedWithSplit_ShouldProduceExpectedResults(string input)
    {
        // Act
        string[] result = GeneratedRegex.MidiTableNameRegex().Split(input);

        // Assert
        Assert.That(result.Length, Is.GreaterThan(1), "Split should produce multiple parts");
        Assert.That(result.Any(part => part.Contains("Content")), Is.True, "Should contain the content between table names");
        Assert.That(result.Any(part => part.Contains("More content")), Is.True, "Should contain the content after table names");
    }
}
