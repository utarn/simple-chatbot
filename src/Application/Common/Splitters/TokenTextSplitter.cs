namespace OpenAiService.Splitters;

public class TokenTextSplitter : TextSplitter
{
    private readonly Func<string, List<int>> _encode;
    private readonly Func<List<int>, string> _decode;

    public TokenTextSplitter(
        Func<string, List<int>> encode,
        Func<List<int>, string> decode,
        int chunkSize = 4000,
        int chunkOverlap = 200,
        bool stripWhitespace = true)
        : base(
            chunkSize: chunkSize,
            chunkOverlap: chunkOverlap,
            lengthFunction: text => encode(text).Count,
            stripWhitespace: stripWhitespace)
    {
        _encode = encode;
        _decode = decode;
    }

    public override List<string> SplitText(string text)
    {
        var tokens = _encode(text);
        var chunks = new List<string>();
        int start = 0;
        
        while (start < tokens.Count)
        {
            int end = Math.Min(start + ChunkSize, tokens.Count);
            var chunkTokens = tokens.Skip(start).Take(end - start).ToList();
            chunks.Add(_decode(chunkTokens));
            start += Math.Max(ChunkSize - ChunkOverlap, 0);
        }

        if (StripWhitespace)
        {
            chunks = chunks.Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();
        }

        return chunks;
    }
}
