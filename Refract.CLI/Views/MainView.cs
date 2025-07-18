using Refract.CLI.Data;
using Refract.CLI.Services;
using Refract.CLI.Utilities;
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
        _tileView.Tiles.ToList()[0].ContentView!.Add(new AskFrameView(ragService, _outputFrameView));
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

        if (ApplicationContext.BinaryFilePath is null) return;

        _statusBar.Add(new Shortcut(Key.F2, "Decompile", ActionDecompileBinary));
        _statusBar.Add(new Shortcut(Key.F3, "Generate chunks", ActionGenerateChunks));
        _statusBar.Add(new Shortcut(Key.F4, "Embed chunks", ActionEmbedChunksWithVectors));
        _statusBar.Add(new Shortcut(Key.F5, "Store chunks", StoreChunksWithVectors));
    }

    private async void StoreChunksWithVectors()
    {
        try
        {
            if (ApplicationContext.BinaryFilePath is null)
            {
                return;
            }

            var chunks = _chunker.LoadChunksFromDirectory(ApplicationContext.ChunkFolderPath);

            await _vectorDbService.ProcessChunksAsync(
                chunks,
                ApplicationContext.ChunkFolderPath,
                ApplicationContext.SessionName
            );

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
            if (ApplicationContext.BinaryFilePath is null)
            {
                return;
            }

            var chunks = _chunker.LoadChunksFromDirectory(ApplicationContext.ChunkFolderPath);
            chunks = await _embeddingService.EmbedChunksAsync(chunks);
            _chunker.SaveChunks(chunks, ApplicationContext.ChunkFolderPath);

            MessageDialog.Show("Success", "All chunks embedded successfully");
        }
        catch (Exception e)
        {
            MessageDialog.Show("Exception occured!", e.Message);
        }
    }

    private void ActionGenerateChunks()
    {
        if (ApplicationContext.BinaryFilePath is null)
        {
            return;
        }

        var chunks = new List<Chunk>();
        chunks.AddRange(_chunker.CreateChunks(File.ReadAllText(ApplicationContext.CFilePath), "C"));
        chunks.AddRange(_chunker.CreateChunks(File.ReadAllText(ApplicationContext.AsmFilePath), "ASM"));
        chunks.AddRange(_chunker.CreateChunks(File.ReadAllText(ApplicationContext.HexFilePath), "HEX"));

        _chunker.SaveChunks(chunks, ApplicationContext.ChunkFolderPath);

        MessageDialog.Show("Success", $"Created {chunks.Count} chunks in {ApplicationContext.ChunkFolderPath} folder.");
    }

    private async void ActionDecompileBinary()
    {
        try
        {
            if (ApplicationContext.BinaryFilePath is null)
            {
                return;
            }

            await _decompileService.DecompileAsync(
                ApplicationContext.BinaryFilePath,
                ApplicationContext.ProjectFolderPath!
            );

            _analyzeTabView.SetAsmTabContent(
                string.Join("\n",
                    File.Exists(ApplicationContext.AsmFilePath)
                        ? await File.ReadAllLinesAsync(ApplicationContext.AsmFilePath)
                        : [])
            );

            _analyzeTabView.SetCTabContent(
                string.Join("\n",
                    File.Exists(ApplicationContext.CFilePath)
                        ? await File.ReadAllLinesAsync(ApplicationContext.CFilePath)
                        : [])
            );

            _analyzeTabView.SetHexTabContent(
                File.Exists(ApplicationContext.HexFilePath)
                    ? File.OpenRead(ApplicationContext.HexFilePath)
                    : new MemoryStream()
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

                var fileBytes = await File.ReadAllBytesAsync(path);
                var crc32 = HashUtilities.CalculateCrc32(fileBytes);
                ApplicationContext.SessionName = crc32.ToString("X8");

                ApplicationContext.ProjectFolderPath = Path.Combine(
                    ApplicationContext.SessionsFolderPath,
                    ApplicationContext.SessionName
                );

                if (!Directory.Exists(ApplicationContext.ProjectFolderPath))
                    Directory.CreateDirectory(ApplicationContext.ProjectFolderPath);

                // Copy target file into project folder
                ApplicationContext.BinaryFilePath =
                    Path.Combine(ApplicationContext.ProjectFolderPath, Path.GetFileName(path));
                File.Copy(path, ApplicationContext.BinaryFilePath, true);

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