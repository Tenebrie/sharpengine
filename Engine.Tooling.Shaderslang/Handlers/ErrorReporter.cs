using Engine.Tooling.Shaderslang.Parser;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server;
using Range = OmniSharp.Extensions.LanguageServer.Protocol.Models.Range;

namespace Engine.Tooling.Shaderslang.Handlers;

public class ErrorReporter(ILanguageServerFacade facade)
{
    public void ReportErrors(DocumentUri documentUri, string documentText)
    {
        ShaderlangParser.ParseDocument(documentText, out var errors);
        
        var diags = errors
            .Select(err => new Diagnostic
            {
                Range = new Range(
                    new Position(err.LineIndex, err.CharIndex),
                    new Position(err.LineIndex, err.CharIndex + err.Length)
                ),
                Severity = DiagnosticSeverity.Error,
                Message = err.Message,
                RelatedInformation = new Container<DiagnosticRelatedInformation> (
                    err.AdditionalInformation.Select(info =>
                        new DiagnosticRelatedInformation
                        {
                            Location = new Location
                            {
                                Uri = documentUri,
                                Range = new Range(
                                    new Position(err.LineIndex, err.CharIndex),
                                    new Position(err.LineIndex, err.CharIndex + err.Length)
                                )
                            },
                            Message = info
                        }
                    )
                )
            })
            .ToArray();
        
        facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = documentUri,
            Diagnostics = new Container<Diagnostic>(diags)
        });
    }
    
    public void ClearErrors(DocumentUri documentUri)
    {
        facade.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = documentUri,
            Diagnostics = new Container<Diagnostic>()
        });
    }
}