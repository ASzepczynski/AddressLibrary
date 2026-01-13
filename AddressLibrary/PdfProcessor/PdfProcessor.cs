using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;
using CsvHelper;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using System.Diagnostics;

public static partial class PdfProcessor
{
    // Regex to detect a word that starts with an uppercase letter followed by all lowercase letters (Unicode-aware)
    internal static readonly Regex CapitalizedWordRegex = new Regex("^\\p{Lu}\\p{Ll}+$", RegexOptions.Compiled);
    // Regex to detect a word that is all lowercase letters
    internal static readonly Regex LowercaseWordRegex = new Regex("^\\p{Ll}+$", RegexOptions.Compiled);
    // Regex to detect lowercase letters followed by a hyphen at the end
    internal static readonly Regex LowercaseHyphenRegex = new Regex("^\\p{Ll}+-$", RegexOptions.Compiled);
}

