using MediatR;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Plugin.Shaderslang.Enums;
using Plugin.Shaderslang.Handlers;

namespace Plugin.Shaderslang;

internal class TextDocumentSyncHandler(ILanguageServerFacade facade, DocumentManager documentManager) : ITextDocumentSyncHandler
{
    private readonly ErrorReporter _errorReporter = new(facade);
    private DocumentManager DocumentManager { get; } = documentManager;

    public TextDocumentAttributes GetTextDocumentAttributes(DocumentUri uri) => new(uri, nameof(LanguageSyntax.Shaderslang));
    
    // =========================================================================================================================
    // Open handler
    // =========================================================================================================================
    Task<Unit> IRequestHandler<DidOpenTextDocumentParams, Unit>.Handle(DidOpenTextDocumentParams request, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine($"Opened: {request.TextDocument.Uri}");
        var document = DocumentManager.LoadBuffer(request.TextDocument.Uri);
        _errorReporter.ReportErrors(request.TextDocument.Uri, document);
        return Unit.Task;
    }

    TextDocumentOpenRegistrationOptions IRegistration<TextDocumentOpenRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentOpenRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(nameof(LanguageSyntax.Shaderslang))
        };
    }

    // =========================================================================================================================
    // Change handler
    // =========================================================================================================================
    Task<Unit> IRequestHandler<DidChangeTextDocumentParams, Unit>.Handle(DidChangeTextDocumentParams request, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine($"Changed: {request.TextDocument.Uri}");
        foreach (var change in request.ContentChanges)
        {
            DocumentManager.UpdateBuffer(request.TextDocument.Uri, change.Text);
        }
        var document = DocumentManager.GetBuffer(request.TextDocument.Uri)!;
        _errorReporter.ReportErrors(request.TextDocument.Uri, document);
        return Unit.Task;
    }

    TextDocumentChangeRegistrationOptions IRegistration<TextDocumentChangeRegistrationOptions, TextSynchronizationCapability>
        .GetRegistrationOptions(TextSynchronizationCapability capability, ClientCapabilities clientCapabilities)
    {
        return new TextDocumentChangeRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(nameof(LanguageSyntax.Shaderslang)),
            SyncKind = TextDocumentSyncKind.Full,
        };
    }
    
    // =========================================================================================================================
    // Save handler
    // =========================================================================================================================
    public Task<Unit> Handle(DidSaveTextDocumentParams request, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine($"Saved: {request.TextDocument.Uri}");
        var document = DocumentManager.LoadBuffer(request.TextDocument.Uri);
        _errorReporter.ReportErrors(request.TextDocument.Uri, document);
        return Task.FromResult(Unit.Value);
    }

    TextDocumentSaveRegistrationOptions IRegistration<TextDocumentSaveRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentSaveRegistrationOptions()
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(nameof(LanguageSyntax.Shaderslang)),
            IncludeText = true
        };
    }
    
    // =========================================================================================================================
    // Close handler
    // =========================================================================================================================
    public Task<Unit> Handle(DidCloseTextDocumentParams request, CancellationToken cancellationToken)
    {
        Console.Error.WriteLine($"Closed: {request.TextDocument.Uri}");
        _errorReporter.ClearErrors(request.TextDocument.Uri);
        return Unit.Task;
    }

    TextDocumentCloseRegistrationOptions IRegistration<TextDocumentCloseRegistrationOptions, TextSynchronizationCapability>.GetRegistrationOptions(TextSynchronizationCapability capability,
        ClientCapabilities clientCapabilities)
    {
        return new TextDocumentCloseRegistrationOptions
        {
            DocumentSelector = TextDocumentSelector.ForLanguage(nameof(LanguageSyntax.Shaderslang))
        };
    }
}