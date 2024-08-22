using System.Text.RegularExpressions;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;
using Page = UglyToad.PdfPig.Content.Page;

namespace RoMi.Business.Models;

internal static class MidiDocumentationFile
{
    internal static async Task<MidiDocument> Parse(string pdfPath)
    {
        return await Task.Run(() => {
            using PdfDocument document = PdfDocument.Open(pdfPath);
            List<Page> pages = document.GetPages().ToList();
            int pageStartIndex = 0;
            string? deviceName = null;

            if (pages.Count == 0)
            {
                throw new Exception();
            }

            // Find model id and index of page that contains "Parameter Address Map" chapter
            for (int i = 0; i < pages.Count; i++)
            {
                string text = ContentOrderTextExtractor.GetText(pages[i], false);
                // PDF-parser result contains OS-specific line breaks -> always use linux style
                text = text.Replace("\r", "");

                // 1. Find the model id
                if (string.IsNullOrEmpty(deviceName))
                {
                    GroupCollection matchCollection = GeneratedRegex.ModelRegex().Match(text).Groups;

                    if (matchCollection.Count != 2)
                    {
                        continue;
                    }

                    deviceName = matchCollection[1].Value;
                }

                // 2. Find the parameter address map caption
                if (!GeneratedRegex.ParameterAddressMapCaption().IsMatch(text))
                {
                    continue;
                }

                pageStartIndex = i;
                break;
            }

            if (string.IsNullOrEmpty(deviceName))
            {
                throw new Exception("Device name (Model) could not be found.");
            }

            if (pageStartIndex == 0)
            {
                throw new Exception("Parameter Address Map could not be found.");
            }

            // get text of all pages starting at index of "Parameter Address Map"
            string textContent = string.Empty;

            for (int i = pageStartIndex; i < pages.Count; i++)
            {
                string pageContent = ContentOrderTextExtractor.GetText(pages[i], false);

                // PDF-parser result contains OS-specific line breaks -> always use linux style
                pageContent = pageContent.Replace("\r", "");

                if (!pageContent.EndsWith('\n'))
                {
                    // make sure each page ends with a line break, as tables beginning on a new page might be parsed as part of the table on the previous page
                    pageContent += "\n";
                }

                textContent += pageContent;
            }

            return new MidiDocument(deviceName, textContent);
        });
    }
}
