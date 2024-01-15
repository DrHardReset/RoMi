using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Page = UglyToad.PdfPig.Content.Page;

namespace RoMi.Business.Models
{
    internal static class MidiDocumentationFile
    {
        internal static async Task<MidiDocument> Parse(string pdfPath)
        {
            return await Task.Run(() => {
                byte[] bytes = File.ReadAllBytes(pdfPath);
                using PdfDocument document = PdfDocument.Open(bytes);
                List<Page> pages = document.GetPages().ToList();
                int pageStartIndex = 0;
                string deviceName = "null";

                if (pages.Count == 0)
                {
                    throw new Exception();
                }

                for (int i = 0; i < pages.Count; i++)
                {
                    string text = ContentOrderTextExtractor.GetText(pages[i], false);
                    // PDF-parser result contains OS-specific line breaks -> always use linux style
                    text = text.Replace("\r", "");

                    if (i == 0) // device name (Model) is expected to be found on first page
                    {
                        GroupCollection matchCollection = Regex.Match(text, @"Model:\s*(.*)[\n]+").Groups;

                        if (matchCollection.Count != 2)
                        {
                            throw new Exception("Device name (Model) could not be found.");
                        }

                        deviceName = matchCollection[1].Value;
                    }

                    if (!Regex.IsMatch(text, @"\d+\. Parameter Address Map"))
                    {
                        continue;
                    }

                    pageStartIndex = i;
                    break;
                }

                if (pageStartIndex == 0)
                {
                    throw new Exception("Parameter Address Map could not be found.");
                }

                string textContent = string.Empty;

                for (int i = pageStartIndex; i < pages.Count; i++)
                {
                    textContent += ContentOrderTextExtractor.GetText(pages[i], false);
                    // PDF-parser result contains OS-specific line breaks -> always use linux style
                    textContent = textContent.Replace("\r", "");
                }

                return new MidiDocument(deviceName, textContent);
            });
        }
    }
}
