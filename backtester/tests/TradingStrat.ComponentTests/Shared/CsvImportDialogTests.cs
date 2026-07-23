using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Shouldly;
using TradingStrat.ComponentTests.Infrastructure;
using TradingStrat.Web.Components.Shared;
using Xunit;

namespace TradingStrat.ComponentTests.Shared;

/// <summary>
/// Tests for the CsvImportDialog component.
/// </summary>
public class CsvImportDialogTests : BunitTestContext
{
    [Fact]
    public void CsvImportDialog_HiddenWhenClosed()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, false)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldBeEmpty();
    }

    [Fact]
    public void CsvImportDialog_ShowsWhenOpen()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldNotBeEmpty();
        cut.Markup.ShouldContain("Import Tickers from CSV");
    }

    [Fact]
    public void CsvImportDialog_HasModalAriaAttributes()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IElement modal = cut.Find("[role='dialog']");
        modal.ShouldNotBeNull();
        modal.GetAttribute("aria-modal").ShouldBe("true");
        modal.GetAttribute("aria-labelledby").ShouldBe("modal-title");
    }

    [Fact]
    public void CsvImportDialog_DisplaysInstructions()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        cut.Markup.ShouldContain("CSV Format");
        cut.Markup.ShouldContain("One ticker per line");
        cut.Markup.ShouldContain("comma-separated");
    }

    [Fact]
    public void CsvImportDialog_HasFileInputWithCorrectAccept()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IRenderedComponent<InputFile> fileInput = cut.FindComponent<InputFile>();
        fileInput.ShouldNotBeNull();
        fileInput.Instance.AdditionalAttributes?["accept"].ShouldBe(".csv,.txt");
    }

    [Fact]
    public void CsvImportDialog_ImportButtonDisabledWhenNoTickers()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IRenderedComponent<Button> importButtonComponent = cut.FindComponents<Button>().First(b => b.Instance.Text == "Import");
        importButtonComponent.Instance.Disabled.ShouldBeTrue();
    }

    [Fact]
    public void CsvImportDialog_CancelButtonClosesDialog()
    {
        // Arrange
        bool closeCalled = false;
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => closeCalled = true)));

        // Act
        IRenderedComponent<Button> cancelButtonComponent = cut.FindComponents<Button>().First(b => b.Instance.Text == "Cancel");
        cancelButtonComponent.Find("button").Click();

        // Assert
        closeCalled.ShouldBeTrue();
    }

    [Fact]
    public void CsvImportDialog_CloseButtonHasCorrectAriaLabel()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IElement closeButton = cut.Find("button[aria-label='Close']");
        closeButton.ShouldNotBeNull();
    }

    [Fact]
    public void CsvImportDialog_HasBackdrop()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IElement backdrop = cut.Find(".bg-gray-500.bg-opacity-75");
        backdrop.ShouldNotBeNull();
    }

    [Fact]
    public void CsvImportDialog_HasCorrectModalStyling()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IElement modalContainer = cut.Find(".fixed.inset-0.z-50");
        modalContainer.ShouldNotBeNull();
    }

    [Fact]
    public void CsvImportDialog_HasHeaderBorderAndPadding()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IElement header = cut.Find(".border-b");
        header.ShouldNotBeNull();
        header.ClassList.ShouldContain("px-6");
        header.ClassList.ShouldContain("py-4");
    }

    [Fact]
    public void CsvImportDialog_HasFooterWithButtons()
    {
        // Arrange & Act
        IRenderedComponent<CsvImportDialog> cut = Render<CsvImportDialog>(parameters => parameters
            .Add(p => p.IsOpen, true)
            .Add(p => p.OnTickersImported, EventCallback.Factory.Create<List<string>>(this, _ => { }))
            .Add(p => p.OnClose, EventCallback.Factory.Create(this, () => { })));

        // Assert
        IElement footer = cut.Find(".border-t");
        footer.ShouldNotBeNull();
        cut.Markup.ShouldContain("Cancel");
        cut.Markup.ShouldContain("Import");
    }
}
