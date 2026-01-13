using System.Text.RegularExpressions;
using UglyToad.PdfPig.Content;

public static partial class PdfProcessor
{
    private static bool AreSameToken(Word prev, Word current)
    {
        if (prev == null || current == null) return false;

        // detect if current is a single capitalized word (Uppercase followed by all lowercase)
        var isCapitalizedWord = CapitalizedWordRegex.IsMatch(current.Text);


        if (isCapitalizedWord && prev.Text.EndsWith(")"))return false;
        if (prev.Text.ToString().EndsWith(",") && Regex.IsMatch(current.Text.ToString(),@"^\d")) return true;
        if (prev.Text.ToString().EndsWith("-")) return true;

        if (prev.Text.ToString().EndsWith("DK")) return false;
        if (prev.Text.ToString().EndsWith(",")) return false;
        if (prev.Text.ToString().EndsWith("Z¹bkowicki")) return false;
        if (prev.Text.ToString() == "32") return false;
        if (prev.Text.ToString() == "42") return false;
        if (prev.Text.ToString() == "-20a") return false;
        if (prev.Text.ToString() == "14-20") return false;
        if (prev.Text.ToString() == "33-36b") return false;
        if (prev.Text.ToString() == "148a") return false;
        if (prev.Text.ToString() == "Nowowiejskiego" && current.Text.ToString() == "Gdynia") return false;
        if (prev.Text.ToString() == "\"Nil\"" && current.Text.ToString() == "Nysa") return false;
        // centers Y
        var aCenterY = (prev.BoundingBox.Top + prev.BoundingBox.Bottom) / 2.0;
        var bCenterY = (current.BoundingBox.Top + current.BoundingBox.Bottom) / 2.0;

        // vertical tolerance based on max height
        var yTolerance = Math.Max(prev.BoundingBox.Height, current.BoundingBox.Height) * 0.5;
        if (System.Math.Abs(aCenterY - bCenterY) > yTolerance) return false;

        // horizontal gap between right edge of prev and left edge of current
        var gap = current.BoundingBox.Left - prev.BoundingBox.Right;

        // average char width estimate for prev
        var avgCharWidthPrev = prev.Text.Length > 0 ? prev.BoundingBox.Width / System.Math.Max(1, prev.Text.Length) : prev.BoundingBox.Width;

        // if gap is small relative to average character width, consider same token
        if (gap <= avgCharWidthPrev * 1.5) return true;

        // otherwise, not the same token
        return false;
    }
}