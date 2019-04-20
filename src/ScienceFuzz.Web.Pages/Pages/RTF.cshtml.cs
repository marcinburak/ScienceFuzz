﻿using CsvHelper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ScienceFuzz.Models;
using ScienceFuzz.Web.Pages.CsvMaps;
using ScienceFuzz.Web.Pages.Tools;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ScienceFuzz.Web.Pages.Pages
{
    public class RTFModel : PageModel
    {
        public class InputModel
        {
            [Required]
            public string AuthorSurname { get; set; }

            [Required]
            public string FormalTypes { get; set; }

            [Required]
            [ValidFileExtensions("rtf")]
            public IFormFile Rtf { get; set; }
        }


        [BindProperty]
        public InputModel Input { get; set; }
        public List<Publication> Publications { get; set; } = new List<Publication>();



        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string rtvString = string.Empty;

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var isoEncoding = Encoding.GetEncoding("iso-8859-2");

            using (var stream = Input.Rtf.OpenReadStream())
            {


                rtvString = await new StreamReader(stream, isoEncoding).ReadToEndAsync();
                rtvString = HttpUtility.HtmlDecode(rtvString);
            }

            var publicationRegex = new Regex(@"<BR> \d*\. <BR>.+?txt end");
            var publicationStrings = publicationRegex.Matches(rtvString).Select(m => m.Value).ToArray();

            foreach (var publicationString in publicationStrings)
            {
                var authorsRegex = new Regex(@"Aut\.:.+?<BR>");
                var authors = authorsRegex.Match(publicationString).Value
                    .Replace("Aut.: </span>", "")
                    .Replace("<BR>", "")
                    .Split(',')
                    .Select(a => a.Trim())
                        .ToArray();

                if (!authors.Any(a => a.Contains(Input.AuthorSurname)))
                {
                    continue;
                }

                var formalTypeRegex = new Regex(@"Typ formalny publikacji: </span>.+?<BR>");
                var formalType = formalTypeRegex.Match(publicationString).Value
                   .Replace("Typ formalny publikacji: </span>", "")
                   .Replace("<BR>", "")
                   .Trim();

                if (!string.IsNullOrWhiteSpace(Input.FormalTypes) &&
                    !Input.FormalTypes.Split(',').Contains(formalType))
                {
                    continue;
                }

                var titleRegex = new Regex(@"Tytuł: </span>.+?<BR>");
                var title = titleRegex.Match(publicationString).Value
                    .Replace("Tytuł: </span>", "")
                    .Replace("<BR>", "")
                    .Trim();

                var journalShortRegex = new Regex(@"Czasopismo: </span>.+?<BR>");
                var journalShort = journalShortRegex.Match(publicationString).Value
                    .Replace("Czasopismo: </span>", "")
                    .Replace("<BR>", "")
                    .Trim();

                var journalFullRegex = new Regex(@"Pełny tytuł czasop.: </span>.+?<BR>");
                var journalFull = journalFullRegex.Match(publicationString).Value
                   .Replace("Pełny tytuł czasop.: </span>", "")
                   .Replace("<BR>", "")
                   .Trim();

                Publications.Add(new Publication
                {
                    Author = Input.AuthorSurname,
                    Title = title,
                    JournalShort = journalShort,
                    JournalFull = journalFull,
                    FormalType = formalType
                });
            }

            var memory = new MemoryStream();
            var writer = new StreamWriter(memory, isoEncoding);
            var csv = new CsvWriter(writer);

            csv.Configuration.CultureInfo = new CultureInfo("pl-PL");
            csv.Configuration.RegisterClassMap<PublicationMap>();

            csv.WriteRecords(Publications);
            memory.Position = 0;

            return File(memory, "file/csv", "data.csv");
        }
    }
}
