using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Refract.CLI.Views;

public sealed class MessageDialog : Dialog
{
    private MessageDialog(string title, string body)
    {
        Title = title;
        X = Pos.Percent(10);
        Y = Pos.Percent(10);
        Width = Dim.Percent(80);
        Height = Dim.Percent(80);

        var btnOk = new Button
        {
            Text = "Ok",
            IsDefault = true
        };

        btnOk.Accepting += (s, e) =>
        {
            e.Handled = true;
            Application.RequestStop();
        };

        var textView = new TextView
        {
            Text = body,
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()! - 2,
            ReadOnly = true,
            AllowsTab = false,
        };

        Buttons = [btnOk];
        Add(textView);
    }

    public static void Show(string title, string body)
    {
        var dlg = new MessageDialog(title, body);
        Application.Run(dlg);
    }
}