using System.Text.RegularExpressions;

namespace OpenAiService.Splitters;

public class CharacterTextSplitter : TextSplitter
{
    private readonly string _separator;
    private readonly bool _isSeparatorRegex;

    public CharacterTextSplitter(
        string separator = "\n\n",
        bool isSeparatorRegex = false,
        int chunkSize = 4000,
        int chunkOverlap = 200,
        Func<string, int>? lengthFunction = null,
        KeepSeparatorMode keepSeparator = KeepSeparatorMode.None,
        bool stripWhitespace = true)
        : base(chunkSize, chunkOverlap, lengthFunction, keepSeparator, stripWhitespace)
    {
        _separator = separator;
        _isSeparatorRegex = isSeparatorRegex;
    }

    public override List<string> SplitText(string text)
    {
        var separator = _isSeparatorRegex ? _separator : Regex.Escape(_separator);
        var splits = SplitTextWithRegex(text, separator);
        return MergeSplits(splits, KeepSeparator == KeepSeparatorMode.None ? "" : _separator);
    }

    private List<string> SplitTextWithRegex(string text, string separator)
    {
        if (string.IsNullOrEmpty(separator))
            return new List<string> { text };

        var splits = new List<string>();
        if (KeepSeparator != KeepSeparatorMode.None)
        {
            var matches = Regex.Matches(text, $"({separator})");
            int lastPos = 0;
            
            foreach (Match match in matches)
            {
                if (match.Index > lastPos)
                {
                    splits.Add(text.Substring(lastPos, match.Index - lastPos));
                }
                splits.Add(match.Value);
                lastPos = match.Index + match.Length;
            }
            
            if (lastPos < text.Length)
            {
                splits.Add(text.Substring(lastPos));
            }

            // Process keep separator logic
            var processed = new List<string>();
            for (int i = 0; i < splits.Count; i++)
            {
                if (i % 2 == 1) // separator
                {
                    if (KeepSeparator == KeepSeparatorMode.End && processed.Count > 0)
                    {
                        processed[^1] += splits[i];
                    }
                    else if (KeepSeparator == KeepSeparatorMode.Start && i < splits.Count - 1)
                    {
                        splits[i + 1] = splits[i] + splits[i + 1];
                    }
                }
                else // content
                {
                    processed.Add(splits[i]);
                }
            }
            splits = processed;
        }
        else
        {
            splits = Regex.Split(text, separator)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (StripWhitespace)
        {
            splits = splits.Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        return splits;
    }
}
