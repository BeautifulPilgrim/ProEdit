using ProEdit.Controls.Skia;
using ProEdit.Documents;
using ProEdit.Editing;
using Xunit;

namespace ProEdit.Controls.Skia.Tests;

public sealed class ProEditDocumentHostSessionTests
{
    [Fact]
    public void LoadSessionSharesDocumentAndLayout()
    {
        using var owner = new ProEditDocumentHost();
        using var adopter = new ProEditDocumentHost();
        owner.LoadDocument(CreateDocument("Shared"));

        adopter.LoadSession(owner.EditorSession);

        Assert.Same(owner.Document, adopter.Document);
        Assert.Same(owner.Layout, adopter.Layout);
    }

    [Fact]
    public void MutationThroughOwnerRaisesChangedOnAdopter()
    {
        using var owner = new ProEditDocumentHost { IsReadOnly = false };
        using var adopter = new ProEditDocumentHost();
        owner.LoadDocument(CreateDocument("Hello"));
        adopter.LoadSession(owner.EditorSession);
        var adopterChanged = 0;
        adopter.Changed += (_, _) => adopterChanged++;
        owner.HandleKey(EditorKey.End, EditorKeyEventKind.Down, EditorModifiers.None);

        var handled = owner.HandleTextInput(" world".AsSpan(), EditorModifiers.None);

        Assert.True(handled);
        Assert.True(adopterChanged > 0);
        Assert.Equal("Hello world", DocumentEditHelpers.GetParagraphText(adopter.Document.GetParagraph(0)));
    }

    [Fact]
    public void AdopterKeepsIndependentViewportAndZoom()
    {
        using var owner = new ProEditDocumentHost();
        using var adopter = new ProEditDocumentHost();
        owner.LoadDocument(CreateDocument("Shared"));
        adopter.LoadSession(owner.EditorSession);

        adopter.SetViewport(300f, 200f);
        owner.SetViewport(700f, 500f);
        adopter.SetZoom(2f);
        owner.SetZoom(0.5f);

        Assert.Equal(300f, adopter.ViewportWidth);
        Assert.Equal(200f, adopter.ViewportHeight);
        Assert.Equal(2f, adopter.Zoom);
        Assert.Equal(700f, owner.ViewportWidth);
        Assert.Equal(500f, owner.ViewportHeight);
        Assert.Equal(0.5f, owner.Zoom);
    }

    [Fact]
    public void ReadOnlyAdopterRejectsMutations()
    {
        using var owner = new ProEditDocumentHost();
        using var adopter = new ProEditDocumentHost { IsReadOnly = true };
        owner.LoadDocument(CreateDocument("Locked"));
        adopter.LoadSession(owner.EditorSession);

        var handled = adopter.HandleTextInput("x".AsSpan(), EditorModifiers.None);

        Assert.False(handled);
        Assert.Equal("Locked", DocumentEditHelpers.GetParagraphText(adopter.Document.GetParagraph(0)));
    }

    [Fact]
    public void AdopterDisposeDoesNotAffectOwnerSession()
    {
        using var owner = new ProEditDocumentHost { IsReadOnly = false };
        owner.LoadDocument(CreateDocument("Owned"));
        var adopter = new ProEditDocumentHost();
        adopter.LoadSession(owner.EditorSession);
        owner.HandleKey(EditorKey.End, EditorKeyEventKind.Down, EditorModifiers.None);

        adopter.Dispose();
        owner.InsertText("!");

        Assert.Equal("Owned!", DocumentEditHelpers.GetParagraphText(owner.Document.GetParagraph(0)));
    }

    [Fact]
    public void LoadSessionIsIdempotentForSameSession()
    {
        using var owner = new ProEditDocumentHost();
        using var adopter = new ProEditDocumentHost();
        owner.LoadDocument(CreateDocument("Shared"));
        adopter.LoadSession(owner.EditorSession);
        adopter.SetScroll(0f, 10f);
        var changedCount = 0;
        adopter.Changed += (_, _) => changedCount++;

        adopter.LoadSession(owner.EditorSession);

        Assert.Equal(0, changedCount);
        Assert.Equal(10f, adopter.ScrollY);
    }

    [Fact]
    public void LoadSessionRegistersWordEditorServicesForAdoptedSession()
    {
        using var owner = new ProEditDocumentHost();
        using var adopter = new ProEditDocumentHost();
        owner.LoadDocument(CreateDocument("Shared"));

        adopter.LoadSession(owner.EditorSession);

        Assert.True(adopter.TryGetService<IUndoRedoService>(out _));
        Assert.True(adopter.TryGetService<IProofingService>(out _));
        Assert.True(adopter.TryGetService<IEditorCommandRouter>(out var commandRouter));
        Assert.Same(adopter.CommandRouter, commandRouter);
    }

    private static Document CreateDocument(string text)
    {
        var document = new Document();
        document.Blocks.Clear();
        document.Blocks.Add(new ParagraphBlock(text));
        return document;
    }
}
