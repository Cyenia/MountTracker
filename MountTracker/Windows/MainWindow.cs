using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using MountTracker.Classes;

namespace MountTracker.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private string mountSearch = string.Empty;
    private string playerSearch = string.Empty;
    private string mountPlayerSearch = string.Empty;
    private uint currentMount;
    private Player? currentPlayer;

    public MainWindow(Plugin plugin) : base("Mount Tracker###main_42069")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(220, 160) * ImGuiHelpers.GlobalScale,
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
        using var configTab = ImRaii.TabBar("##MainTabBar");
        
        DrawMountTab();
        DrawPlayerTab();
    }

    private void DrawMountTab()
    {
        using var tabMount = ImRaii.TabItem("Mounts", ImGuiTabItemFlags.NoCloseWithMiddleMouseButton);
        if (!tabMount.Success) return;
        
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
            DrawSearchFilter("mountSearch", ref mountSearch);
            DrawMountSelectable();

            return;
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        DrawSearchFilter("mountSearch", ref mountSearch);

        using var excelSelectionListBox = ImRaii.ListBox("excelSelectionListBox", ImGui.GetContentRegionAvail());
        DrawMountSelectable();
    }

    private static void DrawSearchFilter(string id, ref string search)
    {
        ImGui.InputTextWithHint("###filter" + id, "Search...", ref search, 256);
    }

    private void DrawMountSelectable()
    {
        var curFilteredMounts = plugin.Configuration.TrackedMounts
                                       .Where(id => plugin.Configuration.Mounts[id].ToString().Contains(mountSearch.ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase))
                                       .ToList();

        Helpers.DrawCollapsableMountSelectable(curFilteredMounts, plugin.Configuration, ref currentMount, true);
    }

    private void DrawCurrentMount()
    {
        var mount = plugin.Configuration.Mounts[currentMount];
        var mountString = $"{string.Join(", ", mount.Fights.Values)}";

        using var child = ImRaii.Child("Mount#" + currentMount, Vector2.Zero, true);

        if (child.Success)
        {
            var textWidth = ImGui.CalcTextSize("All Acquired").X;
            var checkWidth = ImGui.GetFrameHeight();
            var currentCursorPosX =  ImGui.GetCursorPosX();
            
            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - textWidth - checkWidth);
            
            ImGui.AlignTextToFramePadding();
            ImGui.Text("All Acquired");
            ImGui.SameLine();
            
            var allAcquired = plugin.Configuration.Player.All(p => p.IsObtained(currentMount));
            if (ImGui.Checkbox("###" + currentPlayer + "AllAcquired", ref allAcquired))
            {
                plugin.Configuration.Player.ForEach(p => p.SetObtained(currentMount, allAcquired));
                plugin.Configuration.Save();
            }
            
            ImGui.SameLine();
            ImGui.SetCursorPosX(currentCursorPosX);
            
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
                        player.SetObtained(currentMount, playerData);
                        plugin.Configuration.Save();
                    }
                }
            }
        }
    }

    private void DrawPlayerTab()
    {
        using var tabMount = ImRaii.TabItem("Player", ImGuiTabItemFlags.NoCloseWithMiddleMouseButton);
        if (!tabMount.Success) return;
        
        if (currentPlayer == null)
        {
            DrawPlayerSelection();
        }
        else
        {
            DrawPlayerSelection(true);
            try
            {
                DrawCurrentPlayer();
            }
            catch (Exception)
            {
                // ignore
            }
        }
    }

    private void DrawPlayerSelection(bool combo = false)
    {
        if (combo)
        {
            if (ImGuiComponents.IconButton("combo-back", FontAwesomeIcon.ArrowLeft))
            {
                currentPlayer = null;
                return;
            }

            ImGui.SameLine();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var previewValue = currentPlayer != null
                                   ? currentPlayer.ToString()
                                   : "Select a player";
            using var sheetCombo = ImRaii.Combo("###playerCombo", previewValue);
            if (!sheetCombo) return;

            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            DrawSearchFilter("playerSearch", ref playerSearch);
            DrawPlayerSelectable();

            return;
        }

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        DrawSearchFilter("playerSearch", ref playerSearch);

        using var excelSelectionListBox = ImRaii.ListBox("excelSelectionListBox", ImGui.GetContentRegionAvail());
        DrawPlayerSelectable();
    }

    private void DrawPlayerSelectable()
    {
        var curFilteredPlayer = plugin.Configuration.Player
                                      .Where(player => player.ToString().Contains(playerSearch.ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase))
                                      .ToList();

        if(curFilteredPlayer.Count == 0)
        {
            ImGui.Text("No player found.");
            return;
        }
        foreach (var player in curFilteredPlayer)
        {
                    
            if(ImGui.Selectable(player.ToString(), player == currentPlayer))
            {
                currentPlayer = player;
            }
        }
    }

    private void DrawCurrentPlayer()
    {
        using var child = ImRaii.Child("Player#" + currentPlayer, Vector2.Zero, true);

        if (child.Success)
        {
            var textWidth = ImGui.CalcTextSize("All Acquired").X;
            var checkWidth = ImGui.GetFrameHeight();
            var currenCursorPosX = ImGui.GetCursorPosX();

            ImGui.SetCursorPosX(ImGui.GetContentRegionAvail().X - textWidth - checkWidth);
            ImGui.AlignTextToFramePadding();
            ImGui.Text("All Acquired");
            ImGui.SameLine();

            var curFilteredMounts = plugin.Configuration.TrackedMounts
                                          .Where(id => plugin.Configuration.Mounts[id].ToString().Contains(mountPlayerSearch.ToLowerInvariant(), StringComparison.InvariantCultureIgnoreCase))
                                          .ToList();

            var allAcquired = curFilteredMounts.All(m => currentPlayer!.IsObtained(m));
            if (ImGui.Checkbox("###" + currentPlayer + "AllAcquired", ref allAcquired))
            {
                curFilteredMounts.ForEach(m => currentPlayer!.SetObtained(m, allAcquired));
                plugin.Configuration.Save();
            }

            ImGui.SameLine();
            ImGui.SetCursorPosX(currenCursorPosX);
            
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - textWidth - checkWidth - (ImGui.GetStyle().ItemSpacing.X*2));
            DrawSearchFilter("mountPlayerSearch", ref mountPlayerSearch);
            
            using var table = ImRaii.Table("Mounts##" + currentPlayer, 2,
                                           ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH);

            if (table.Success)
            {
                ImGui.TableSetupColumn("Mount###mountNameCol", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Acquired  ###mountAcquired", ImGuiTableColumnFlags.WidthFixed);
                
                ImGui.TableHeadersRow();

                if(curFilteredMounts.Count == 0)
                {
                    ImGui.Text("No mounts found.");
                    return;
                }
                foreach (var mount in curFilteredMounts)
                {
                    var mountData = plugin.Configuration.Mounts[mount];
                    
                    ImGui.TableNextColumn();
                    ImGui.Text(mountData.ToString());

                    ImGui.TableNextColumn();
                    var playerData = currentPlayer!.IsObtained(mount);
                    if (ImGui.Checkbox("##" + currentPlayer + mount, ref playerData))
                    {
                        currentPlayer.SetObtained(mount, playerData);
                        plugin.Configuration.Save();
                    }
                }
            }
        }
    }
}
