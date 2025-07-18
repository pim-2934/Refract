using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Refract.CLI.Views;

public class AnalyzeTabView : TabView
{
    private readonly Tab _cTab = new();
    private readonly Tab _asmTab = new();
    private readonly Tab _hexTab = new();

    private readonly TextView _cTextView = new();
    private readonly TextView _asmTextView = new();
    private readonly HexView _hexView = new();

    public AnalyzeTabView()
    {
        Width = Dim.Percent(100);
        Height = Dim.Percent(100);

        _cTab.DisplayText = "C";
        _cTab.View = _cTextView;

        _cTextView.Width = Dim.Percent(100);
        _cTextView.Height = Dim.Percent(100);

        _asmTab.DisplayText = "ASM";
        _asmTab.View = _asmTextView;

        _asmTextView.Width = Dim.Percent(100);
        _asmTextView.Height = Dim.Percent(100);

        _hexTab.DisplayText = "HEX";
        _hexTab.View = _hexView;

        _hexView.Width = Dim.Percent(100);
        _hexView.Height = Dim.Percent(100);

        AddTab(_cTab, true);
        AddTab(_asmTab, false);
        AddTab(_hexTab, false);
    }

    public void SetCTabContent(string text)
    {
        _cTextView.Text = text;
    }

    public void SetAsmTabContent(string text)
    {
        _asmTextView.Text = text;
    }

    public void SetHexTabContent(Stream stream)
    {
        _hexView.Source = stream; // TODO: data has to be formatted differently for this view to function
    }
}