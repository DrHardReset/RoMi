using System.Text.RegularExpressions;

namespace RoMi.Tests;

[TestFixture]
internal class ParseModelIdTests
{
    [Test]
    [TestCase("AX-Edge (ModelID = 00H 00H 00 52H)\n", new string[] { "00", "00", "00", "52" })]
    [TestCase("he model\nID of the exclusive messages used by this instrument is 00H 00H 00H 79H.", new string[] { "00", "00", "00", "79" })] // SPD-SX PRO
    public void ModelId_Match(string text, string[] modelIdBytes)
    {
        // Act
        IEnumerable<Group> groups = GeneratedRegex.ModelIdBytesRegex().Match(text).Groups.Cast<Group>().Skip(1);
        string[] array = [.. groups.Where(o => o.Value != string.Empty).Select(o => o.Value.ToString())];

        // Assert
        Assert.Multiple(delegate
        {
            Assert.That(array.Count, Is.EqualTo(modelIdBytes.Length));

            for (int i = 0; i < modelIdBytes.Length; i++)
            {
                Assert.That(array[i], Is.EqualTo(modelIdBytes[i]));
            }
        });
    }
}
