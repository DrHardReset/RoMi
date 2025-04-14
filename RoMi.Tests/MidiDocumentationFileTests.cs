namespace RoMi.Tests;

[TestFixture]
internal class MidiDocumentationFileTests
{
    [Test, Explicit]
    [TestCase(@".\JD-Xi_MIDI_Imple_e01_W.pdf")]
    public void ParsingPdfSucceeds(string pdfPath)
    {
        // Act & Assert
        Assert.DoesNotThrow(delegate { MidiDocumentationFile.Parse(pdfPath).Wait(); });
    }
}
