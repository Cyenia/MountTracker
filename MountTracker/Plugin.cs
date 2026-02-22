using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Lumina.Excel.Sheets;
using LuminaSupplemental.Excel.Model;
using LuminaSupplemental.Excel.Services;
using MountTracker.Services;
using MountTracker.Windows;

namespace MountTracker;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/pmount";

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem = new("MountTracker");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin(IDalamudPluginInterface pluginInterface)
    {
        pluginInterface.Create<PluginServices>();
        
        Configuration = PluginServices.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        PluginServices.CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Opens the mount tracking window."
        });

        PluginServices.PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginServices.PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginServices.PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        AddTrackedMounts();
        _ = AddMounts();

#if DEBUG
        if(PluginServices.PluginInterface.IsDevMenuOpen) ToggleConfigUi();
#endif
    }

    public void Dispose()
    {
        PluginServices.PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginServices.PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginServices.PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        PluginServices.CommandManager.RemoveHandler(CommandName);
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.Toggle();
    }

    private void AddTrackedMounts() 
    {
        foreach (var player in Configuration.Player)
        {
            player.AddMounts(Configuration.TrackedMounts.ToList());
        }
    }

    private async Task AddMounts() 
    {
        var gameData = PluginServices.DataManager.GameData;
        var dungeonBossChests = CsvLoader.LoadResource<DungeonBossChest>(CsvLoader.DungeonBossChestResourceName, true, out _, out _, gameData, gameData.Options.DefaultExcelLanguage);
        var itemSheet = gameData.GetExcelSheet<Item>();
        if (itemSheet != null)
        {
            foreach (var item in itemSheet)
            {
                if (item is { FilterGroup: 16, ItemSortCategory.Value.Param: 175, ItemUICategory.RowId: 63 })
                {
                    var mount = new Classes.Mount(item.RowId);
                    var drop = dungeonBossChests.Where(x => x.ItemId == item.RowId);

                    foreach (var chest in drop)
                    {
                        var fight = new Classes.Fight(chest.ContentFinderConditionId);
                        mount.AddFight(fight);
                    }

                    if (mount.GetFights().Count != 0)
                    {
                        if (Configuration.Mounts.TryAdd(mount.MountId, mount))
                        {
                            Configuration.Save();
                            await Task.Yield();
                        }
                    }
                }
            }
        }
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
