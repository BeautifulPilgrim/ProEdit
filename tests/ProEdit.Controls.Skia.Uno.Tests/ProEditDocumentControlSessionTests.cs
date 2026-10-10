using ProEdit.Controls.Skia.Uno;
using ProEdit.Documents;
using ProEdit.Editing;
using Xunit;

namespace ProEdit.Controls.Skia.Uno.Tests;

public sealed class ProEditDocumentControlSessionTests
{
    [Fact]
    public void EditorSession_StartsAsHostSession()
    {
        if (!UnoUiTestEnvironment.IsUiRuntimeAvailable())
        {
            return;
        }

        var editor = new ProEditDocumentEditor();

        Assert.NotNull(editor.EditorSession);
        Assert.Same(editor.DocumentHost.EditorSession, editor.EditorSession);
    }

    [Fact]
    public void Document_Replace_UpdatesEditorSession()
    {
        if (!UnoUiTestEnvironment.IsUiRuntimeAvailable())
        {
            return;
        }

        var editor = new ProEditDocumentEditor();
        var initialSession = editor.EditorSession;

        editor.Document = CreateDocument("Replaced");

        Assert.NotSame(initialSession, editor.EditorSession);
        Assert.Same(editor.Document, editor.DocumentHost.Document);
    }

    [Fact]
    public void Session_Adopt_ReplacesHostSessionAndDocument()
    {
        if (!UnoUiTestEnvironment.IsUiRuntimeAvailable())
        {
            return;
        }

        var editor = new ProEditDocumentEditor();
        var viewer = new ProEditDocumentViewer();
        var document = CreateDocument("Shared");
        editor.Document = document;

        viewer.Session = editor.DocumentHost.EditorSession;

        Assert.Same(document, viewer.Document);
        Assert.Same(document, viewer.DocumentHost.Document);
        Assert.Same(editor.DocumentHost.EditorSession, viewer.DocumentHost.EditorSession);
        Assert.Same(editor.DocumentHost.Layout, viewer.DocumentHost.Layout);
    }

    [Fact]
    public void Session_Null_FallsBackToFreshDocument()
    {
        if (!UnoUiTestEnvironment.IsUiRuntimeAvailable())
        {
            return;
        }

        var editor = new ProEditDocumentEditor();
        var viewer = new ProEditDocumentViewer();
        editor.Document = CreateDocument("Shared");
        viewer.Session = editor.DocumentHost.EditorSession;

        viewer.Session = null;

        Assert.NotNull(viewer.Document);
        Assert.NotSame(editor.DocumentHost.EditorSession, viewer.DocumentHost.EditorSession);
    }

    [Fact]
    public void Document_EqualToAdoptedSessionDocument_DoesNotBreakSharing()
    {
        if (!UnoUiTestEnvironment.IsUiRuntimeAvailable())
        {
            return;
        }

        var editor = new ProEditDocumentEditor();
        var viewer = new ProEditDocumentViewer();
        var document = CreateDocument("Shared");
        editor.Document = document;
        viewer.Session = editor.DocumentHost.EditorSession;

        viewer.Document = document;

        Assert.Same(editor.DocumentHost.EditorSession, viewer.DocumentHost.EditorSession);
    }

    [Fact]
    public void SharedSession_PropagatesMutationsToAdopter()
    {
        if (!UnoUiTestEnvironment.IsUiRuntimeAvailable())
        {
            return;
        }

        var editor = new ProEditDocumentEditor { IsReadOnly = false };
        var viewer = new ProEditDocumentViewer();
        editor.Document = CreateDocument("Hello");
        viewer.Session = editor.DocumentHost.EditorSession;
        var viewerChanged = 0;
        viewer.DocumentHost.Changed += (_, _) => viewerChanged++;

        var handled = editor.DocumentHost.HandleTextInput(" world".AsSpan(), EditorModifiers.None);

        Assert.True(handled);
        Assert.True(viewerChanged > 0);
        Assert.Equal(
            DocumentEditHelpers.GetParagraphText(editor.DocumentHost.Document.GetParagraph(0)),
            DocumentEditHelpers.GetParagraphText(viewer.DocumentHost.Document.GetParagraph(0)));
    }

    private static Document CreateDocument(string text)
    {
        var document = new Document();
        document.Blocks.Clear();
        document.Blocks.Add(new ParagraphBlock(text));
        return document;
    }
}
