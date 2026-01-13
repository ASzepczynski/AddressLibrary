using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using AddressLibrary.Models;
using AddressLibrary.Data;

public static partial class PdfProcessor
{
    /// <summary>
    /// Finds best matching record for a given address components.
    /// Returns tuple: (matchedRecord or null, score).
    /// </summary>
    public static (Pna? Match, int Score) FindBestMatch(
        AddressDbContext context,
        string kod,
        string miasto,
        string ulica,
        string numerDomu,
        string numerMieszkania)
    {
        // prepare normalized forms for comparison
        string normPostal = Normalize(kod);
        string normCity = Normalize(miasto);
        string normStreet = Normalize(ulica);
        string normBuilding = Normalize(numerDomu);

        // scoring
        Pna? best = null;
        int bestScore = -1;

        var records = context.Pna.Where(x => x.Miasto==normCity).ToList();

        foreach (var r in records)
        {
            int score = 0;
            
            // compare postal (r.Kod may contain code and city; try to extract code)
            var rPostalMatch = Regex.Match(r.Kod ?? string.Empty, "\\b(\\d{2}-\\d{3})\\b");
            var rPostal = rPostalMatch.Success ? rPostalMatch.Groups[1].Value : string.Empty;
            if (!string.IsNullOrEmpty(normPostal) && Normalize(rPostal) == normPostal) 
                score += 50;

            // compare city
            //if (!string.IsNullOrEmpty(normCity) && Normalize(r.Miasto) == normCity) 
            //    score += 30;

            // compare street
            var rStreetNorm = Normalize(r.Ulica ?? string.Empty);
            if (!string.IsNullOrEmpty(normStreet))
            {
                // prefer exact street name equality strongly
                if (rStreetNorm == normStreet)
                {
                    score += 100; // strong boost for exact match
                }
                else if (rStreetNorm.Contains(normStreet) || normStreet.Contains(rStreetNorm))
                {
                    score += 20; // weaker bonus for partial/substring matches
                }
            }

            // if building present try match against r.Numery
            if (!string.IsNullOrEmpty(normBuilding) && !string.IsNullOrEmpty(r.Numery))
            {
                var normalizedNumery = Normalize(r.Numery);
                if (normalizedNumery.Contains(normBuilding))
                    score += 10;
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = r;
            }
        }

        return (best, bestScore);
    }

    private static string Normalize(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        s = s.Trim().ToLowerInvariant();
        // remove diacritics
        var normalized = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var ch in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(ch);
            }
        }
        var result = sb.ToString().Normalize(NormalizationForm.FormC);
        // remove punctuation
        result = Regex.Replace(result, "[\\p{P}\"]+", "");
        // collapse spaces
        result = Regex.Replace(result, "\\s+", " ").Trim();
        return result;
    }
}
