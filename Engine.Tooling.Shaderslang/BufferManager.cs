
using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Engine.Tooling.Shaderslang;

public class DocumentManager
{
    private readonly ConcurrentDictionary<DocumentUri, string> _buffers = new();

    public string LoadBuffer(DocumentUri document)
    {
        var path = document.Path;
        var buffer = File.ReadAllText(path[1..]);
        UpdateBuffer(document, buffer);
        return buffer;
    }
    
    public void UpdateBuffer(DocumentUri document, string buffer)
    {
        _buffers.AddOrUpdate(document, buffer, (k, v) => buffer);
    }

    public string? GetBuffer(DocumentUri document)
    {
        var val = _buffers.GetValueOrDefault(document);
        if (val is null)
        {
            return LoadBuffer(document);
        }
        return val;
    }
}