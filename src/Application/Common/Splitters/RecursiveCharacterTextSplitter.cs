using System.Text.RegularExpressions;

namespace OpenAiService.Splitters;

public class RecursiveCharacterTextSplitter : TextSplitter
{
    private readonly List<string> _separators;
    private readonly KeepSeparatorMode _keepSeparator;
    private readonly bool _isSeparatorRegex;
    private readonly int _chunkSize;
    private readonly int _chunkOverlap;
    private readonly Func<string, int> _lengthFunction;
    private readonly bool _stripWhitespace;

    public RecursiveCharacterTextSplitter(
        List<string>? separators = null,
        KeepSeparatorMode keepSeparator = KeepSeparatorMode.None,
        bool isSeparatorRegex = false,
        int chunkSize = 4000,
        int chunkOverlap = 200,
        Func<string, int>? lengthFunction = null,
        bool stripWhitespace = true)
    {
        if (chunkOverlap > chunkSize)
            throw new ArgumentException("Chunk overlap must be smaller than chunk size");

        _separators = separators ?? new List<string> { "\n\n", "\n", " ", "" };
        _keepSeparator = keepSeparator;
        _isSeparatorRegex = isSeparatorRegex;
        _chunkSize = chunkSize;
        _chunkOverlap = chunkOverlap;
        _lengthFunction = lengthFunction ?? ((s) => s.Length);
        _stripWhitespace = stripWhitespace;
    }

    public override List<string> SplitText(string text)
    {
        return SplitTextInternal(text, _separators);
    }

    private List<string> SplitTextInternal(string text, List<string> separators)
    {
        var finalChunks = new List<string>();
        var separator = separators.Last();
        var newSeparators = new List<string>();

        // Find the first separator that exists in the text
        foreach (var s in separators)
        {
            var pattern = _isSeparatorRegex ? s : Regex.Escape(s);
            if (s == "")
            {
                separator = s;
                break;
            }

            if (Regex.IsMatch(text, pattern))
            {
                separator = s;
                var index = separators.IndexOf(s);
                newSeparators = separators.Skip(index + 1).ToList();
                break;
            }
        }

        // Split using the selected separator
        var regexPattern = _isSeparatorRegex ? separator : Regex.Escape(separator);
        var splits = SplitWithRegex(text, regexPattern, _keepSeparator);
        var mergedSeparator = _keepSeparator == KeepSeparatorMode.None ? "" : separator;

        var goodSplits = new List<string>();
        foreach (var split in splits)
        {
            if (_lengthFunction(split) < _chunkSize)
            {
                goodSplits.Add(split);
            }
            else
            {
                if (goodSplits.Count > 0)
                {
                    var merged = MergeSplits(goodSplits, mergedSeparator);
                    finalChunks.AddRange(merged);
                    goodSplits.Clear();
                }

                if (newSeparators.Count == 0)
                {
                    finalChunks.Add(split);
                }
                else
                {
                    var recursiveSplit = SplitTextInternal(split, newSeparators);
                    finalChunks.AddRange(recursiveSplit);
                }
            }
        }

        if (goodSplits.Count > 0)
        {
            var merged = MergeSplits(goodSplits, mergedSeparator);
            finalChunks.AddRange(merged);
        }

        return finalChunks;
    }

    private List<string> SplitWithRegex(string text, string separatorPattern, KeepSeparatorMode keepSeparator)
    {
        var splits = new List<string>();

        if (string.IsNullOrEmpty(separatorPattern))
        {
            splits.Add(text);
            return splits;
        }

        if (keepSeparator != KeepSeparatorMode.None)
        {
            var parts = Regex.Split(text, $"({separatorPattern})");
            var filtered = parts.Where(p => !string.IsNullOrEmpty(p)).ToList();

            for (int i = 0; i < filtered.Count; i++)
            {
                if (keepSeparator == KeepSeparatorMode.End && i % 2 == 0 && i < filtered.Count - 1)
                {
                    splits.Add(filtered[i] + filtered[i + 1]);
                    i++;
                }
                else if (keepSeparator == KeepSeparatorMode.Start && i % 2 == 1 && i > 0)
                {
                    splits.Add(filtered[i - 1] + filtered[i]);
                }
                else
                {
                    splits.Add(filtered[i]);
                }
            }
        }
        else
        {
            splits = Regex.Split(text, separatorPattern)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        if (_stripWhitespace)
        {
            splits = splits.Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        return splits;
    }

    private List<string> MergeSplits(List<string> splits, string separator)
    {
        var merged = new List<string>();
        var currentDoc = new List<string>();
        int totalLength = 0;
        int separatorLength = _lengthFunction(separator);

        foreach (var split in splits)
        {
            var splitLength = _lengthFunction(split);
            var potentialLength = totalLength + 
                                  (currentDoc.Count > 0 ? separatorLength : 0) + 
                                  splitLength;

            if (potentialLength > _chunkSize)
            {
                if (currentDoc.Count > 0)
                {
                    merged.Add(JoinDocs(currentDoc, separator));
                    
                    // Remove elements from start until under chunk overlap
                    while (totalLength > _chunkOverlap || 
                           (potentialLength > _chunkSize && totalLength > 0))
                    {
                        totalLength -= _lengthFunction(currentDoc[0]) + 
                                       (currentDoc.Count > 1 ? separatorLength : 0);
                        currentDoc.RemoveAt(0);
                        potentialLength = totalLength + 
                                          (currentDoc.Count > 0 ? separatorLength : 0) + 
                                          splitLength;
                    }
                }
            }

            currentDoc.Add(split);
            totalLength += splitLength + 
                           (currentDoc.Count > 1 ? separatorLength : 0);
        }

        if (currentDoc.Count > 0)
        {
            merged.Add(JoinDocs(currentDoc, separator));
        }

        return merged;
    }

    protected override string JoinDocs(List<string> docs, string separator)
    {
        var joined = string.Join(separator, docs);
        return _stripWhitespace ? joined.Trim() : joined;
    }
}
