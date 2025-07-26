
using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using OmniSharp.Extensions.LanguageServer.Protocol;

namespace Plugin.Shaderslang;

public class BufferManager
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