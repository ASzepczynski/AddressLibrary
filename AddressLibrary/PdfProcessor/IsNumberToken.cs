public static partial class PdfProcessor
{
    private static bool IsNumberToken(ReadOnlySpan<char> s, out string? cleaned)
    {
        cleaned = null;
        int len = s.Length;
        if (len == 0) return false;

        // trim leading/trailing whitespace (but do not strip commas here)
        int start = 0;
        int end = len - 1;
        while (start <= end && char.IsWhiteSpace(s[start])) start++;
        while (end >= start && char.IsWhiteSpace(s[end])) end--;
        if (start > end) return false;

        var span = s.Slice(start, end - start + 1);
        int spanLen = span.Length;

        int i = 0;
        // must start with digit
        if (i >= spanLen || !char.IsDigit(span[i])) return false;

        // consume digits
        while (i < spanLen && char.IsDigit(span[i])) i++;
        // consume optional letters
        while (i < spanLen && char.IsLetter(span[i])) i++;

        // If we've consumed entire span -> simple form
        if (i == spanLen)
        {
            cleaned = span.ToString();
            // remove trailing comma if present (defensive)
            cleaned = cleaned.Trim();
            if (cleaned.EndsWith(",")) cleaned = cleaned.Substring(0, cleaned.Length - 1).Trim();
            return cleaned.Length > 0;
        }

        // skip spaces after the number/letters
        int j = i;
        while (j < spanLen && char.IsWhiteSpace(span[j])) j++;

        // case: starts-with-comma (digits/letters then comma) -> treat as number, keep the initial part
        if (j < spanLen && span[j] == ',')
        {
            var part = span.Slice(0, i).ToString().Trim();
            if (part.Length == 0) return false;
            cleaned = part;
            return true;
        }

        // case: range form with '-'
        if (j < spanLen && span[j] == '-')
        {
            // ensure something after '-'
            int k = j + 1;
            while (k < spanLen && char.IsWhiteSpace(span[k])) k++;
            if (k >= spanLen) return false;

            cleaned = span.ToString().Trim();
            // remove trailing comma if present
            if (cleaned.EndsWith(",")) cleaned = cleaned.Substring(0, cleaned.Length - 1).Trim();
            return cleaned.Length > 0;
        }

        if (span.ToString().Contains("/"))
        {
            cleaned = span.ToString().Trim();
            return true;
        }
        // not a number token
        return false;
    }
}