namespace OpenAiService.Splitters;

public abstract class TextSplitter
{
    protected int ChunkSize { get; }
    protected int ChunkOverlap { get; }
    protected Func<string, int> LengthFunction { get; }
    protected KeepSeparatorMode KeepSeparator { get; }
    protected bool StripWhitespace { get; }

    protected TextSplitter(
        int chunkSize = 4000,
        int chunkOverlap = 200,
        Func<string, int>? lengthFunction = null,
        KeepSeparatorMode keepSeparator = KeepSeparatorMode.None,
        bool stripWhitespace = true)
    {
        if (chunkOverlap > chunkSize)
            throw new ArgumentException("Chunk overlap must be smaller than chunk size");

        ChunkSize = chunkSize;
        ChunkOverlap = chunkOverlap;
        LengthFunction = lengthFunction ?? (s => s.Length);
        KeepSeparator = keepSeparator;
        StripWhitespace = stripWhitespace;
    }

    public abstract List<string> SplitText(string text);

    protected virtual List<string> MergeSplits(List<string> splits, string separator)
    {
        var merged = new List<string>();
        var currentDoc = new List<string>();
        int total = 0;
        int separatorLen = LengthFunction(separator);

        foreach (var d in splits)
        {
            var len = LengthFunction(d);
            if (total + len + (currentDoc.Count > 0 ? separatorLen : 0) > ChunkSize)
            {
                if (currentDoc.Count > 0)
                {
                    var doc = JoinDocs(currentDoc, separator);
                    if (doc != null)
                        merged.Add(doc);
                    
                    while (total > ChunkOverlap || 
                           (total + len + (currentDoc.Count > 0 ? separatorLen : 0) > ChunkSize && total > 0))
                    {
                        total -= LengthFunction(currentDoc[0]) + 
                                 (currentDoc.Count > 1 ? separatorLen : 0);
                        currentDoc.RemoveAt(0);
                    }
                }
            }
            
            currentDoc.Add(d);
            total += len + (currentDoc.Count > 1 ? separatorLen : 0);
        }

        var finalDoc = JoinDocs(currentDoc, separator);
        if (finalDoc != null)
            merged.Add(finalDoc);

        return merged;
    }

    protected virtual string JoinDocs(List<string> docs, string separator)
    {
        var text = string.Join(separator, docs);
        if (StripWhitespace)
            text = text.Trim();
        return string.IsNullOrEmpty(text) ? string.Empty : text!;
    }
}
