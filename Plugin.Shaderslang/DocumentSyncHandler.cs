using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;

namespace Plugin.Shaderslang;

internal class TextDocumentSyncHandler(BufferManager bufferManager) : ITextDocumentSyncHandler
{
    private BufferManager BufferManager { get; } = bufferManager;
    
    private readonly TextDocumentSyncKind _kind = TextDocumentSyncKind.Full;
    private readonly ITextDocumentRegistrationOptions _registrationOptions = new TextDocumentOpenRegistrationOptions();

    public TextDocumentChangeRegistrationOptions ChangeRegistrationOptions =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("shaderslang"),
            SyncKind = _kind,
        };

    public Task<Unit> Handle(DidOpenTextDocumentParams request, CancellationToken token)
    {
        Console.Error.WriteLine($"LanguageId: {request.TextDocument.LanguageId}");
        Console.Error.WriteLine($"Opened: {request.TextDocument.Uri}");
        return Unit.Task;
    }

    public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri)
        => new TextDocumentAttributes(uri, "shaderslang");

    public Task<Unit> Handle(DidChangeTextDocumentParams request, 
        CancellationToken token)
    {
        Console.Error.WriteLine($"Changed: {request.TextDocument.Uri}");
        return Unit.Task;
    }

    Task<Unit> IRequestHandler<DidChangeTextDocumentParams, Unit>.Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        bufferManager.LoadBuffer(request.TextDocument.Uri);
        return Handle(request, cancellationToken);
    }

    TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions, TextSynchronizationCapability>
        .GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentChangeRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("shaderslang"),
            SyncKind = _kind,
        };
    }

    Task<Unit> IRequestHandler<DidOpenTextDocumentParams, Unit>.Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        bufferManager.LoadBuffer(request.TextDocument.Uri);
        return Handle(request, cancellationToken);
    }

    TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        Console.Error.WriteLine("GetRegistrationOptions called for DidOpenTextDocumentParams");
        return new TextDocumentOpenRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("shaderslang")
        };
    }

    public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine($"Closed: {request.TextDocument.Uri}");
        return Unit.Task;
    }

    TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        Console.Error.WriteLine("GetRegistrationOptions called for DidCloseTextDocumentParams");
        return new TextDocumentCloseRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("shaderslang")
        };
    }

    public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        bufferManager.LoadBuffer(request.TextDocument.Uri);
        Console.Error.WriteLine($"Saved: {request.TextDocument.Uri}");
        return Task.FromResult(Unit.Value);
    }

    TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        Console.Error.WriteLine("Save registration options requested");
        return new TextDocumentSaveRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage("shaderslang"),
            IncludeText = true
        };
    }
}