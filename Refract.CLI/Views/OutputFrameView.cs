using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Refract.CLI.Views;

public sealed class OutputFrameView : FrameView
{
    private readonly TextView _outputView = new();

    public OutputFrameView()
    {
        Title = "Output";
        Width = Dim.Percent(100);
        Height = Dim.Percent(80);

        _outputView.ReadOnly = true;
        _outputView.Width = Dim.Percent(100);
        _outputView.Height = Dim.Percent(100);
        _outputView.WordWrap = true;

        Add(_outputView);
    }

    public void AppendOutput(string text)
    {
        if (_outputView.Text.Length > 0)
            _outputView.Text += $"{Environment.NewLine}---{Environment.NewLine}";

        _outputView.Text += text;
    }
}