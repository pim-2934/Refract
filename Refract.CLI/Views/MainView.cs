using Microsoft.Extensions.Logging;
using Refract.CLI.Entities;
using Refract.CLI.Services;
using Terminal.Gui.App;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;

namespace Refract.CLI.Views;

public sealed class MainView : Toplevel
{
    private readonly ApplicationContext _context;

    private readonly IChunker _chunker;
    private readonly IBinaryAnalyzer _binaryAnalyzer;
    private readonly IEmbedder _embedder;
    private readonly IIndexer _indexer;

    private readonly TileView _tileView = new(2);
    private readonly AnalyzeTabView _analyzeTabView = new();
    private readonly OutputFrameView _outputFrameView = new();
    private readonly StatusBar _statusBar = new();

    public MainView(ApplicationContext context, IChunker chunker, IBinaryAnalyzer binaryAnalyzer, IEmbedder embedder,
        IIndexer indexer,
        RagService ragService, ILogger<MainView> logger)
    {
        _context = context;
        _chunker = chunker;
        _binaryAnalyzer = binaryAnalyzer;
        _embedder = embedder;
        _indexer = indexer;

        logger.LogInformation("MainView created");

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
        _tileView.Tiles.ToList()[0].ContentView!.Add(new AskFrameView(context, ragService, _outputFrameView));
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

        if (!_context.IsActiveSession) return;

        _statusBar.Add(new Shortcut(Key.F2, "Decompile", ActionDecompileBinary));
        _statusBar.Add(new Shortcut(Key.F3, "Generate chunks", ActionGenerateChunks));
        _statusBar.Add(new Shortcut(Key.F4, "Embed chunks", ActionEmbedChunksWithVectors));
        _statusBar.Add(new Shortcut(Key.F5, "Store chunks", StoreChunksWithVectors));
    }

    private async void StoreChunksWithVectors()
    {
        try
        {
            if (!_context.IsActiveSession) return;

            await _indexer.ProcessChunksAsync(_context.Session!);

            MessageDialog.Show("Success", "Chunks stored successfully");
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
            if (!_context.IsActiveSession) return;

            await _embedder.EmbedChunksAsync(_context.Session!.Chunks);
            _context.Session.Save();

            MessageDialog.Show("Success", "All chunks embedded successfully");
        }
        catch (Exception e)
        {
            MessageDialog.Show("Exception occured!", e.Message);
        }
    }

    private async void ActionGenerateChunks()
    {
        try
        {
            if (!_context.IsActiveSession) return;

            var chunks = new List<Chunk>();
            chunks.AddRange(_chunker.CreateChunks(await _binaryAnalyzer.DecompileToCAsync(_context.Session!), "C"));
            chunks.AddRange(
                _chunker.CreateChunks(await _binaryAnalyzer.DisassembleToAsmAsync(_context.Session!), "ASM"));
            chunks.AddRange(_chunker.CreateChunks(await _binaryAnalyzer.DumpHexAsync(_context.Session!), "HEX"));

            _context.Session!.SetChunks(chunks);
            _context.Session!.Save();

            MessageDialog.Show("Success",
                $"Created {_context.Session.Chunks.Count} chunks in {_context.Session.ChunksPath} folder.");
        }
        catch (Exception e)
        {
            MessageDialog.Show("Exception occured!", e.Message);
        }
    }

    private async void ActionDecompileBinary()
    {
        try
        {
            if (!_context.IsActiveSession) return;

            _analyzeTabView.SetAsmTabContent(await _binaryAnalyzer.DisassembleToAsmAsync(_context.Session!));
            _analyzeTabView.SetCTabContent(await _binaryAnalyzer.DecompileToCAsync(_context.Session!));
            _analyzeTabView.SetHexTabContent(await _binaryAnalyzer.DumpHexAsync(_context.Session!));

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

                await _context.StartSession(path);

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