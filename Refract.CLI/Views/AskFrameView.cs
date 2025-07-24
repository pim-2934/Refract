using Refract.CLI.Entities;
using Refract.CLI.Services;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Refract.CLI.Views;

public sealed class AskFrameView() : FrameView
{
    private readonly TextView _askField = new();

    public AskFrameView(ApplicationContext context, RagService ragService, OutputFrameView outputFrameView) : this()
    {
        Title = "Ask";
        Width = Dim.Percent(100);
        Height = Dim.Percent(20);
        Y = Pos.Percent(80);

        _askField.Width = Dim.Percent(100);
        _askField.Height = Dim.Percent(100);
        _askField.AllowsReturn = false;
        _askField.WordWrap = true;
        _askField.Accepting += async (_, args) =>
        {
            try
            {
                args.Handled = true;

                if (!context.IsActiveSession) return;

                var query = _askField.Text;
                _askField.Text = "";

                var response = await ragService.AskAsync(context.Session!, query);

                outputFrameView.AppendOutput(response);
            }
            catch (Exception e)
            {
                MessageDialog.Show("Exception occured!", e.Message);
            }
        };

        Add(_askField);
    }
}