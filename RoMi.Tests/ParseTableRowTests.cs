namespace RoMi.Tests;

[TestFixture]
internal class ParseTableRowTests
{
    [Test]
    [TestCase("| 01 00 00 00 | Setup [Setup] |")]
    [TestCase("| 23 7E 00 00 | User Tone (256)                                        [Tone]  |")]
    [TestCase("| 00 00 00 | Program Common [Program Common] |")]
    [TestCase("| 00 0F | 0000 000a | Audio In Output (0 - 1) |")]
    [TestCase("|# 00 00 | 0000 aaaa | |")]
    [TestCase("| | | -24 - +12 [dB] |")]
    [TestCase("| : | |")]
    [TestCase(": : : :")]
    [TestCase("| 0 | COMPRESSOR | ON OFF |")]
    [TestCase("| 10 | DISTORTION 1 | DRIVE |")]
    public void MiditableContentRow_Match(string row)
    {
        // Act
        bool isMatch = GeneratedRegex.MiditableContentRow().IsMatch(row);

        // Assert
        Assert.That(isMatch, Is.True);
    }

    [Test]
    [TestCase("| 00 00 00 00 | System                                                [System] |\n |", 1)]
    [TestCase("| 00 00 00 00 | System                                                [System] |\n|-------------+----------------------------------------------------------------|\n| 01 00 00 00 | Temporary Program                                    [Program] |\n", 2)]
    [TestCase("|       00 0E | 0aaa aaaa | Reserved                                           |\n:             :           :                                                    :\n|       00 11 | 0000 000a | Receive Program Change                     (0 - 1) |\n", 3)]
    [TestCase("|    00 10 00 | Kit MFX 1                                                [Mfx] |\n|    00 12 00 | Kit MFX 2                                                [Mfx] |\n|          :  |                                                                |\n|    00 16 00 | Kit MFX 4                                                [Mfx] |", 4)]
    public void MiditableContentRow_MultiMatch(string row, int expectedMatchCount)
    {
        // Act
        var matches = GeneratedRegex.MiditableContentRow().Matches(row);
        int matchCount = matches.Count;

        // Assert
        Assert.That(matchCount, Is.EqualTo(expectedMatchCount));
    }

    [Test]
    [TestCase("+------------------------------------------------------------------------------+")]
    [TestCase("|-------------+----------------------------------------------------------------|")]
    [TestCase("* [Setup]")]
    [TestCase("+------+------++------+------++------+------++------+------+")]
    [TestCase("| D | H || D | H || D | H || D | H |")]
    [TestCase("| 0 | 00H || 32 | 20H || 64 | 40H || 96 | 60H |")]
    [TestCase("| | | |")]
    public void MiditableContentRow_NoMatch(string row)
    {
        // Act
        bool isMatch = GeneratedRegex.MiditableContentRow().IsMatch(row);

        // Assert
        Assert.That(isMatch, Is.False);
    }
}
