using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace MountTracker.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private string search = string.Empty;
    private uint currentMount;

    public MainWindow(Plugin plugin) : base("Mount Tracker###42069")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        this.plugin = plugin;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }

    public override void Draw()
    {
        if (currentMount == 0)
        {
            DrawMountSelection();
        }
        else
        {
            DrawMountSelection(true);
            try
            {
                DrawCurrentMount();
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }

    private void DrawMountSelection(bool combo = false)
    {
        if (combo)
        {
            if (ImGuiComponents.IconButton("combo-back", FontAwesomeIcon.ArrowLeft))
            {
                currentMount = 0;
                return;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var previewValue = plugin.Configuration.Mounts.TryGetValue(currentMount, out var mount)
                                   ? mount.ToString()
                                   : "Select a mount";
            using var sheetCombo = ImRaii.Combo("###mountCombo", previewValue);
            if (!sheetCombo) return;

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            DrawSearchFilter();
            DrawMountSelectable();

            return;
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        DrawSearchFilter();

        using var excelSelectionListBox = ImRaii.ListBox("excelSelectionListBox", ImGui.GetContentRegionAvail());
        DrawMountSelectable();
    }

    private void DrawSearchFilter()
    {
        ImGui.InputTextWithHint("###filter", "Search...", ref search, 256);
    }

    private void DrawMountSelectable()
    {
        var curFilteredMounts = plugin.Configuration.TrackedMounts
                                       .Where(id => plugin.Configuration.Mounts[id].ToString().Contains(search.ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase))
                                       .ToList();
        
        foreach (var id in curFilteredMounts.Where(id => ImGui.Selectable(plugin.Configuration.Mounts[id].ToString(), id == currentMount)))
        {
            currentMount = id;
        }
    }

    private void DrawCurrentMount()
    {
        var mount = plugin.Configuration.Mounts[currentMount];
        var mountString = $"{string.Join(", ", mount.Fights.Values)}";

        using var child = ImRaii.Child("Mount#" + currentMount, Vector2.Zero, true);

        if (child.Success)
        {
            ImGui.Text(mountString);

            ImGui.Spacing();
            using var table = ImRaii.Table("Player##" + currentMount, 2,
                                           ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH);

            if (table.Success)
            {
                ImGui.TableSetupColumn("Player###playerNameCol", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Acquired  ###mountAcquired", ImGuiTableColumnFlags.WidthFixed);
                
                ImGui.TableHeadersRow();
                
                foreach (var player in plugin.Configuration.Player)
                {
                    ImGui.TableNextColumn();
                    ImGui.Text(player.Name);

                    ImGui.TableNextColumn();
                    var playerData = player.Mounts[currentMount];
                    if (ImGui.Checkbox("##" + currentMount + player, ref playerData))
                    {
                        player.Mounts[currentMount] = playerData;
                        plugin.Configuration.Save();
                    }
                }
            }
        }
    }
}
