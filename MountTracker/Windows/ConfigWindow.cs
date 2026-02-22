using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using MountTracker.Services;

namespace MountTracker.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    
    private string search = string.Empty;
    private uint currentMount;
    
    private string firstname = string.Empty;
    private string lastname = string.Empty;
    private string server = string.Empty;

    public ConfigWindow(Plugin plugin) : base("Mount Tracker - Config###config_42069")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(280, 205) * ImGuiHelpers.GlobalScale,
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        configuration = plugin.Configuration;
    }

    public void Dispose() { }

    public override void PreDraw()
    {
        // Flags must be added or removed before Draw() is being called, or they won't apply
    }

    public override void Draw()
    {
        using var configTab = ImRaii.TabBar("##ConfigTabBar");
        
        DrawMountTab();
        DrawPlayerTab();
    }

    private void DrawMountTab()
    {
        using var tabMount = ImRaii.TabItem("Mounts", ImGuiTabItemFlags.NoCloseWithMiddleMouseButton);
        if (!tabMount.Success) return;
        
        var buttonSize = ImGuiHelpers.GetButtonSize(FontAwesomeIcon.Plus.ToIconString());
        var comboSizeX = ImGui.GetContentRegionAvail().X - (currentMount != 0 ? ImGui.GetStyle().ItemSpacing.X + buttonSize.X : 0);
        
        ImGui.SetNextItemWidth(comboSizeX);
        var previewValue = configuration.Mounts.TryGetValue(currentMount, out var mount)
                               ? mount.ToString()
                               : "Add a mount";
        using (var sheetCombo = ImRaii.Combo("###mountCombo", previewValue))
        {
            if (sheetCombo.Success)
            {
                ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
                DrawSearchFilter();
                DrawMountSelectable();
            }
        }

        if (currentMount != 0)
        {
            ImGui.SameLine();
            
            if (ImGuiComponents.IconButton("combo-back", FontAwesomeIcon.Plus))
            {
                configuration.TrackedMounts.Add(currentMount);
                foreach (var player in configuration.Player)
                {
                    player.AddMount(currentMount);
                }
                configuration.Save();
                
                currentMount = 0;
                return;
            }
        }
        
        ImGui.Spacing();
        
        DrawTrackedMounts();
    }
    
    private void DrawSearchFilter()
    {
        ImGui.InputTextWithHint("###filter", "Search...", ref search, 256);
    }
    
    private void DrawMountSelectable()
    {
        var term = search.ToLowerInvariant();
        var curFilteredMounts = configuration.Mounts
                                                 .Where(keyVal => keyVal.Value.ToString().Contains(term, StringComparison.InvariantCultureIgnoreCase))
                                                 .Select(keyVal => keyVal.Key)
                                                 .ToList();
        
        foreach (var id in curFilteredMounts.Where(id => !configuration.TrackedMounts.Contains(id) && ImGui.Selectable(configuration.Mounts[id].ToString(), id == currentMount)))
        {
            currentMount = id;
        }
    }
    
    private void DrawTrackedMounts()
    {
        if (configuration.TrackedMounts.Count == 0) return;
        
        if (ImGui.CollapsingHeader("Tracked Mounts"))
        {
            using var trackedMountsChild = ImRaii.Child("###trackedMountsChild");
            if (!trackedMountsChild.Success) return;

            using var trackedTable = ImRaii.Table("trackedMountsTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH);
            if (!trackedTable.Success) return;
            
            ImGui.TableSetupColumn("#mountNameCol", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("#deleteMountCol", ImGuiTableColumnFlags.WidthFixed);
            
            foreach (var id in configuration.TrackedMounts)
            {
                if (!configuration.Mounts.TryGetValue(id, out var trackedMount))
                    continue;

                ImGui.TableNextColumn();
                ImGui.Text(trackedMount.ToString());
                ImGui.TableNextColumn();
                if (ImGuiComponents.IconButton($"removeMount#{id}", FontAwesomeIcon.Trash))
                {
                    configuration.TrackedMounts.Remove(id);
                    configuration.Save();
                    return;
                }
            }
        }
    }

    private void DrawPlayerTab()
    {
        using var tabPlayer = ImRaii.TabItem("Player", ImGuiTabItemFlags.NoCloseWithMiddleMouseButton);
        if (!tabPlayer.Success) return;

        DrawAddPlayerManual();
        DrawAddPlayerParty();
        DrawTrackedPlayer();
    }

    private void DrawAddPlayerManual()
    {
        if (ImGui.CollapsingHeader("Add Player manually"))
        {
            using var trackedTable = ImRaii.Table("addPlayerManualTable", 4);
            if (!trackedTable.Success) return;

            ImGui.TableSetupColumn("#addPlayerNameCol", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("#addPlayerLastNameCol", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("#addPlayerServerCol", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("#addPlayerCol", ImGuiTableColumnFlags.WidthFixed);

            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("###firstname", "Firstname", ref firstname, 256);
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("###lastname", "Lastname", ref lastname, 256);
            ImGui.TableNextColumn();
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            ImGui.InputTextWithHint("###server", "Server", ref server, 256);
            ImGui.TableNextColumn();
            var playerContained = configuration.Player.Any(player => player.ToString() == $"{firstname} {lastname}@{server}");
            if (firstname != string.Empty && lastname != string.Empty && server != string.Empty && !playerContained)
            {
                if (ImGuiComponents.IconButton("#addPlayerManually", FontAwesomeIcon.Plus))
                {
                    var player = new Classes.Player($"{firstname} {lastname}", server);
                    player.AddMounts(configuration.TrackedMounts.ToList());
                    configuration.Player.Add(player);
                    configuration.Save();

                    firstname = string.Empty;
                    lastname = string.Empty;
                    server = string.Empty;
                }
            }
            else
            {
                ImGuiComponents.DisabledButton(FontAwesomeIcon.Plus);
                if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                {
                    var toolTipText = playerContained ? "Player already added" : "Fill all fields to add player";
                    ImGui.SetTooltip(toolTipText);
                }
            }
        }
    }
    
    private void DrawAddPlayerParty()
    {
        var currentPlayer = PluginServices.PlayerState;
        var currentPlayerString = $"{currentPlayer.CharacterName}@{currentPlayer.HomeWorld.Value.Name.ExtractText()}";
        var playerContained = configuration.Player.Any(player => player.ToString() == currentPlayerString);
        
        if (ImGui.CollapsingHeader("Add Player from Party"))
        {

            if (PluginServices.PartyList.Count == 0 && playerContained)
            {
                ImGui.Text("Not in a party");
                return;
            }
            
            using var partyTable = ImRaii.Table("addPlayerPartyTable", 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.RowBg);
            if (!partyTable.Success) return;

            ImGui.TableSetupColumn("#addPartyPlayerNameCol", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("#addPartyPlayerCol", ImGuiTableColumnFlags.WidthFixed);

            if (PluginServices.PartyList.Count != 0)
            {
                foreach (var partyMember in PluginServices.PartyList)
                {
                    var name = partyMember.Name.TextValue;
                    var playerServer = partyMember.World.Value.Name.ExtractText();
                    
                    ImGui.TableNextColumn();
                    ImGui.Text($"{name}@{playerServer}");
                    ImGui.TableNextColumn();
                    playerContained = configuration.Player.Any(player => player.ToString() == $"{name}@{playerServer}");
                    if(!playerContained)
                    {
                        if (ImGuiComponents.IconButton("addPartyPlayer#" + partyMember.EntityId, FontAwesomeIcon.Plus))
                        {
                            var player = new Classes.Player(name, playerServer);
                            player.AddMounts(configuration.TrackedMounts.ToList());
                            configuration.Player.Add(player);
                            configuration.Save();
                        }
                    }
                    else
                    {
                        ImGuiComponents.DisabledButton(FontAwesomeIcon.Plus);
                        if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                        {
                            ImGui.SetTooltip("Player already added");
                        }
                    }
                }
            }
            else if (!playerContained)
            {
                ImGui.TableNextColumn();
                ImGui.Text(currentPlayer.CharacterName + "@" + currentPlayer.HomeWorld.Value.Name.ExtractText());
                ImGui.TableNextColumn();
                if (ImGuiComponents.IconButton("addCurrentPlayer#" + currentPlayer.EntityId, FontAwesomeIcon.Plus))
                {
                    var player = new Classes.Player(currentPlayer.CharacterName, currentPlayer.HomeWorld.Value.Name.ExtractText());
                    player.AddMounts(configuration.TrackedMounts.ToList());
                    configuration.Player.Add(player);
                    configuration.Save();
                }
            }
        }
    }

    private void DrawTrackedPlayer()
    {
        if (configuration.Player.Count == 0) return;
        
        if (ImGui.CollapsingHeader("Tracked Player"))
        {
            using var trackedPlayerChild = ImRaii.Child("###trackedPlayerChild");
            if (!trackedPlayerChild.Success) return;

            using var trackedTable = ImRaii.Table("trackedPlayerTable", 2, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH);
            if (!trackedTable.Success) return;
            
            ImGui.TableSetupColumn("#playerNameCol", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("#deletePlayerCol", ImGuiTableColumnFlags.WidthFixed);
            
            foreach (var player in configuration.Player)
            {
                ImGui.TableNextColumn();
                ImGui.Text(player.ToString());
                ImGui.TableNextColumn();
                if(ImGui.IsKeyDown(ImGuiKey.LeftCtrl) || ImGui.IsKeyDown(ImGuiKey.RightCtrl))
                {
                    if (ImGuiComponents.IconButton($"removePlayer#{player}", FontAwesomeIcon.Trash))
                    {
                        configuration.Player.Remove(player);
                        configuration.Save();
                        return;
                    }
                }
                else
                {
                    ImGuiComponents.DisabledButton(FontAwesomeIcon.Trash);
                    if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
                    {
                        ImGui.SetTooltip("Hold CTRL to remove player");
                    }
                }
            }
        }
    }
}
