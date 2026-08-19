using System;
using System.Linq;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace vfaux;

internal enum WeeklyPuzzleTexture
{
    // Background textures
    Hidden = 5,
    Blank = 6,
    Blocked = 9,
}

internal enum WeeklyPuzzlePrizeTexture
{
    TinyBox = 0,
    TinySwords = 1,
    TinyChest = 2,
    TinyCommander = 3,
    BoxTL = 4,
    BoxTR = 5,
    BoxBL = 6,
    BoxBR = 7,
    ChestTL = 8,
    ChestTR = 9,
    ChestBL = 10,
    ChestBR = 11,
    SwordsTL = 12,
    SwordsTR = 13,
    SwordsML = 14,
    SwordsMR = 15,
    SwordsBL = 16,
    SwordsBR = 17,
    Commander = 18,
}

public sealed class Plugin : IDalamudPlugin
{
    public static IPluginLog? Log;

    public IDalamudPluginInterface DalamudPluginInterface { get; init; }
    public ICommandManager CommandManager { get; init; }
    public IAddonLifecycle AddonLifecycle { get; init; }
    public INotificationManager NotificationManager { get; init; }

    private BoardState _board = new();
    private Solver _solver = new();

    public WindowSystem WindowSystem = new("vfaux");
    private PluginWindow _wnd;

    public Plugin(IDalamudPluginInterface dalamud, ICommandManager commmandManager, IPluginLog log, INotificationManager notificationManager, IAddonLifecycle addonLifecycle)
    {
        Log = log;

        // Must run before the window is constructed or the command registered, as those
        // resolve their .Loc() text once at construction time.
        Localization.Init(dalamud.AssemblyLocation.DirectoryName);

        DalamudPluginInterface = dalamud;
        CommandManager = commmandManager;
        NotificationManager = notificationManager;
        AddonLifecycle = addonLifecycle;

        _wnd = new(_board, _solver);
        WindowSystem.AddWindow(_wnd);
        CommandManager.AddHandler("/vfaux",
            new CommandInfo((_, _) => _wnd.IsOpen = true) { HelpMessage = "Show plugin window".Loc() });

        AddonLifecycle.RegisterListener(AddonEvent.PreDraw, "WeeklyPuzzle", SyncWithGameState);
        DalamudPluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        DalamudPluginInterface.UiBuilder.OpenConfigUi += () => _wnd.IsOpen = true;
        DalamudPluginInterface.ActivePluginsChanged += OnActivePluginsChanged;

        if (DalamudPluginInterface.InstalledPlugins.Any((plugin) => plugin.InternalName == "FauxHollowsSolver" && plugin.IsLoaded))
            ShowEzFauxHollowsError();
    }

    public void Dispose()
    {
        DalamudPluginInterface.ActivePluginsChanged -= OnActivePluginsChanged;
        AddonLifecycle.UnregisterListener(SyncWithGameState);
        CommandManager.RemoveHandler("/vfaux");
        WindowSystem.RemoveAllWindows();
    }

    private void OnActivePluginsChanged(IActivePluginsChangedEventArgs args)
    {
        if (args.AffectedInternalNames.Contains("FauxHollowsSolver") && args.Kind == PluginListInvalidationKind.Loaded)
            ShowEzFauxHollowsError();
    }

    private void ShowEzFauxHollowsError()
    {
        NotificationManager.AddNotification(new()
        {
            Title = "Easier Faux Hollows",
            Content = "Easier Faux Hollows is not compatible with ezFauxHollows".Loc(),
            Type = Dalamud.Interface.ImGuiNotification.NotificationType.Error
        });
    }

    private unsafe void SyncWithGameState(AddonEvent type, AddonArgs args)
    {
        if (!args.Addon.IsVisible || !args.Addon.IsReady) return;

        var addon = (AddonWeeklyPuzzle*)args.Addon.Address;
        var tileState = ReadTileStateFromAddon(addon);
        _board.Update(tileState);
        var solution = _solver.Solve(_board);
        var bestScore = solution.Max();
        if (bestScore == 0)
            bestScore = -1;
        UpdateAddonColors(addon, solution, bestScore);
    }

    private unsafe BoardState.Tile[] ReadTileStateFromAddon(AddonWeeklyPuzzle* addon)
    {
        var result = new BoardState.Tile[BoardState.Width * BoardState.Height];
        int tileIndex = 0;
        for (int y = 0; y < BoardState.Height; ++y)
        {
            for (int x = 0; x < BoardState.Width; ++x)
            {
                ref var tileState = ref result[tileIndex++];

                var tileButton = GetTileButton(addon, x, y);
                var tileBackgroundImage = GetBackgroundImageNode(tileButton);
                var tileIconImage = GetIconImageNode(tileButton);

                // 節點取不到就把這格當成未知（Tile.Unknown 是 0，AnalyzeBoard 不會誤判成任何棋型）。
                // 下面每個 -> 都是裸解參考，null 進去就是 AVE；這是 addon 重繪路徑，不逐格寫 log。
                if (tileBackgroundImage == null || tileIconImage == null)
                {
                    tileState = BoardState.Tile.Unknown;
                    continue;
                }

                tileState = (WeeklyPuzzleTexture)tileBackgroundImage->PartId switch
                {
                    WeeklyPuzzleTexture.Hidden => BoardState.Tile.Hidden,
                    WeeklyPuzzleTexture.Blocked => BoardState.Tile.Blocked,
                    WeeklyPuzzleTexture.Blank => !tileIconImage->IsVisible() ? BoardState.Tile.Empty : (WeeklyPuzzlePrizeTexture)tileIconImage->PartId switch
                    {
                        WeeklyPuzzlePrizeTexture.BoxTL => BoardState.Tile.BoxTL,
                        WeeklyPuzzlePrizeTexture.BoxTR => BoardState.Tile.BoxTR,
                        WeeklyPuzzlePrizeTexture.BoxBL => BoardState.Tile.BoxBL,
                        WeeklyPuzzlePrizeTexture.BoxBR => BoardState.Tile.BoxBR,
                        WeeklyPuzzlePrizeTexture.ChestTL => BoardState.Tile.ChestTL,
                        WeeklyPuzzlePrizeTexture.ChestTR => BoardState.Tile.ChestTR,
                        WeeklyPuzzlePrizeTexture.ChestBL => BoardState.Tile.ChestBL,
                        WeeklyPuzzlePrizeTexture.ChestBR => BoardState.Tile.ChestBR,
                        WeeklyPuzzlePrizeTexture.SwordsTL => BoardState.Tile.SwordsTL,
                        WeeklyPuzzlePrizeTexture.SwordsTR => BoardState.Tile.SwordsTR,
                        WeeklyPuzzlePrizeTexture.SwordsML => BoardState.Tile.SwordsML,
                        WeeklyPuzzlePrizeTexture.SwordsMR => BoardState.Tile.SwordsMR,
                        WeeklyPuzzlePrizeTexture.SwordsBL => BoardState.Tile.SwordsBL,
                        WeeklyPuzzlePrizeTexture.SwordsBR => BoardState.Tile.SwordsBR,
                        WeeklyPuzzlePrizeTexture.Commander => BoardState.Tile.Commander,
                        _ => BoardState.Tile.Unknown
                    },
                    _ => BoardState.Tile.Unknown
                };

                if (tileState == BoardState.Tile.Unknown)
                    Log?.Error($"Unexpected tile state at {x}x{y}: bg={tileBackgroundImage->PartId}, icon={tileIconImage->PartId}");

                var rotation = tileIconImage->AtkResNode.Rotation;
                if (rotation < 0)
                    tileState |= BoardState.Tile.RotatedL;
                else if (rotation > 0)
                    tileState |= BoardState.Tile.RotatedR;
            }
        }
        return result;
    }

    private unsafe void UpdateAddonColors(AddonWeeklyPuzzle* addon, int[] solution, int bestScore)
    {
        int tileIndex = 0;
        for (int y = 0; y < BoardState.Height; ++y)
        {
            for (int x = 0; x < BoardState.Width; ++x)
            {
                var soln = solution[tileIndex++];
                var tileButton = GetTileButton(addon, x, y);
                var tileBackgroundImage = GetBackgroundImageNode(tileButton);

                // 取不到就跳過這格的上色（下面是對節點的寫入，null 進去等同寫位址 0 附近）。
                if (tileBackgroundImage == null)
                    continue;

                var (r, g, b) = soln switch
                {
                    Solver.ConfirmedSword => (31, 174, 186),
                    Solver.ConfirmedBoxChest => (180, 173, 44),
                    Solver.PotentialFox => (193, 98, 186),
                    _ => soln == bestScore ? (32, 143, 46) : (0, 0, 0)
                };
                tileBackgroundImage->AddRed = (short)r;
                tileBackgroundImage->AddGreen = (short)g;
                tileBackgroundImage->AddBlue = (short)b;
            }
        }
    }

    private unsafe AtkComponentButton* GetTileButton(AddonWeeklyPuzzle* addon, int x, int y) => addon->GameBoard[y][x].Button;

    /// <summary>
    /// 取出按鈕元件底下寫死索引的影像節點。
    /// 按鈕本身、NodeList、以及索引處的元素三者都可能是空指標（版面重建期間常態），
    /// 而寫死的索引也不保證在 NodeListCount 之內；任何一項不成立就回 null 交給呼叫端判斷——
    /// 直接解參考下去是 AccessViolation，try/catch 攔不到。
    /// </summary>
    private unsafe AtkImageNode* GetImageNode(AtkComponentButton* button, uint index)
    {
        if (button == null || button->UldManager.NodeList == null || button->UldManager.NodeListCount <= index)
            return null;

        var node = button->UldManager.NodeList[index];
        return node == null ? null : (AtkImageNode*)node;
    }

    private unsafe AtkImageNode* GetBackgroundImageNode(AtkComponentButton* button) => GetImageNode(button, 3);
    private unsafe AtkImageNode* GetIconImageNode(AtkComponentButton* button) => GetImageNode(button, 6);
}
