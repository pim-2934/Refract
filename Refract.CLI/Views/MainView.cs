using Refract.CLI.Services;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Refract.CLI.Views;

public sealed class MainView : Toplevel
{
    private readonly IChunker _chunker;
    private readonly DecompileService _decompileService;
    private readonly EmbeddingService _embeddingService;
    private readonly VectorDbService _vectorDbService;

    private readonly TileView _tileView = new(2);
    private readonly AnalyzeTabView _analyzeTabView = new();
    private readonly OutputFrameView _outputFrameView = new();
    private readonly StatusBar _statusBar = new();

    private Session? _session;

    public MainView(
        IChunker chunker,
        DecompileService decompileService,
        RagService ragService,
        EmbeddingService embeddingService,
        VectorDbService vectorDbService)
    {
        _chunker = chunker;
        _decompileService = decompileService;
        _embeddingService = embeddingService;
        _vectorDbService = vectorDbService;

        Width = Dim.Fill();
        Height = Dim.Fill();
        X = 0;
        Y = 0;
        Visible = true;
        Arrangement = ViewArrangement.Overlapped;
        CanFocus = true;
        ShadowStyle = ShadowStyle.None;
        Modal = false;
        TextAlignment = Alignment.Start;

        _tileView.LineStyle = LineStyle.Dotted;
        _tileView.Width = Dim.Percent(100);
        _tileView.Height = Dim.Percent(99);

        _tileView.Tiles.ToList()[0].ContentView!.Add(_outputFrameView);
        _tileView.Tiles.ToList()[0].ContentView!.Add(new AskFrameView(_session, ragService, _outputFrameView));
        _tileView.Tiles.ToList()[1].ContentView!.Add(_analyzeTabView);

        _statusBar.Width = Dim.Fill();
        _statusBar.Height = Dim.Auto();
        _statusBar.X = 0;
        _statusBar.Y = Pos.AnchorEnd(1);
        _statusBar.Visible = true;
        _statusBar.Arrangement = ViewArrangement.Fixed;
        _statusBar.CanFocus = true;

        UpdateShortcuts();

        Add(_tileView);
        Add(_statusBar);
    }

    private void UpdateShortcuts()
    {
        _statusBar.RemoveAll();

        _statusBar.Add(new Shortcut(Key.F1, "Load binary", ActionLoadBinary));

        if (_session is null) return;

        _statusBar.Add(new Shortcut(Key.F2, "Decompile", ActionDecompileBinary));
        _statusBar.Add(new Shortcut(Key.F3, "Generate chunks", ActionGenerateChunks));
        _statusBar.Add(new Shortcut(Key.F4, "Embed chunks", ActionEmbedChunksWithVectors));
        _statusBar.Add(new Shortcut(Key.F5, "Store chunks", StoreChunksWithVectors));
    }

    private async void StoreChunksWithVectors()
    {
        try
        {
            if (_session is null)
                return;

            await _vectorDbService.ProcessChunksAsync(_session);

            MessageDialog.Show("Success", "All chunks stored successfully");
        }
        catch (Exception e)
        {
            MessageDialog.Show("Exception occured!", e.Message);
        }
    }

    private async void ActionEmbedChunksWithVectors()
    {
        try
        {
            if (_session is null)
                return;

            var chunks = await _embeddingService.EmbedChunksAsync(_session.Chunks);
            _session.SetChunks(chunks);
            _session.Save();

            MessageDialog.Show("Success", "All chunks embedded successfully");
        }
        catch (Exception e)
        {
            MessageDialog.Show("Exception occured!", e.Message);
        }
    }

    private void ActionGenerateChunks()
    {
        if (_session is null)
            return;

        _session.Chunks.AddRange(_chunker.CreateChunks(File.ReadAllText(_session.CFilePath), "C"));
        _session.Chunks.AddRange(_chunker.CreateChunks(File.ReadAllText(_session.AsmFilePath), "ASM"));
        _session.Chunks.AddRange(_chunker.CreateChunks(File.ReadAllText(_session.HexFilePath), "HEX"));

        _session.Save();

        MessageDialog.Show("Success", $"Created {_session.Chunks.Count} chunks in {_session.ChunksPath} folder.");
    }

    private async void ActionDecompileBinary()
    {
        try
        {
            if (_session is null)
                return;

            await _decompileService.DecompileAsync(_session, _session.BinaryFilePath, _session.SessionPath);

            _analyzeTabView.SetAsmTabContent(
                string.Join("\n",
                    File.Exists(_session.AsmFilePath) ? await File.ReadAllLinesAsync(_session.AsmFilePath) : [])
            );

            _analyzeTabView.SetCTabContent(
                string.Join("\n",
                    File.Exists(_session.CFilePath) ? await File.ReadAllLinesAsync(_session.CFilePath) : [])
            );

            _analyzeTabView.SetHexTabContent(
                File.Exists(_session.HexFilePath) ? File.OpenRead(_session.HexFilePath) : new MemoryStream()
            );


            MessageDialog.Show("Success", "Binary decompiled!");
        }
        catch (Exception e)
        {
            MessageDialog.Show("Exception occured!", e.Message);
        }
    }

    private async void ActionLoadBinary()
    {
        try
        {
            var ofd = new OpenDialog
            {
                Title = "Open"
            };
            ofd.Layout();

            Application.Run(ofd);

            if (ofd.Canceled) return;

            try
            {
                var path = ofd.Path;

                if (string.IsNullOrEmpty(path))
                    return;

                _session = await Session.InitAsync(path);

                UpdateShortcuts();

                MessageDialog.Show("Success", "Binary loaded");
            }
            catch (Exception e)
            {
                MessageDialog.Show("Exception occured!", e.Message);
            }
        }
        catch (Exception e)
        {
            MessageDialog.Show("Exception occured!", e.Message);
        }
    }
}