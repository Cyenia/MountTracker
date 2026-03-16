using System.Collections.Generic;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Lumina.Excel.Sheets;

namespace MountTracker.Classes;

public static class Helpers
{
    public static void DrawCurrentMountFightsTooltip(OrderedDictionary<uint, Mount> mounts, uint currentMountId)
    {
        if (mounts.TryGetValue(currentMountId, out var mountData))
        {
            if (ImGui.IsItemHovered())
            {
                var tooltip = string.Join("\r\n", mountData.Fights.Values);
                ImGui.SetTooltip(tooltip);
            }
        }
    }

    public static void DrawCollapsableMountSelectable(List<uint> filteredMounts, Configuration config, ref uint currentMountId, bool showPercentage = false)
    {
        OrderedDictionary<uint, ExVersion> exVersions = new();
        Dictionary<uint, SortedSet<uint>> groupedMounts = new();

        float playerCount = config.Player.Count > 0 ? config.Player.Count : 0;
        if(filteredMounts.Count == 0)
        {
            ImGui.Text("No mounts found.");
            return;
        }

        foreach (var mount in filteredMounts)
        {
            var contentFinderCondition = config.Mounts[mount].GetFights().FirstOrDefault().Value?.ToContentFinderCondition();
            
            if (contentFinderCondition != null)
            {
                var exVersion = contentFinderCondition.Value.RequiredExVersion.Value;
                exVersions.TryAdd(exVersion.RowId, exVersion);

                groupedMounts.TryAdd(exVersion.RowId, []);
                groupedMounts[exVersion.RowId].Add(mount);
            }
        }

        foreach (var exVersion in exVersions)
        {
            if (ImGui.CollapsingHeader(exVersion.Value.Name.ExtractText(), ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Indent(15);
                foreach (var mount in groupedMounts[exVersion.Key])
                {
                    var playerObtained = playerCount > 0
                                             ? config.Player.Select(p => p.IsObtained(mount)).Count(obtained => obtained)
                                             : 0;

                    var mountName = config.Mounts[mount].ToString();
                    var mountString = $"{mountName}";
                    if (showPercentage)
                    {
                        mountString += $" ({playerObtained / playerCount * 100:F0}%)";
                    }

                    if(ImGui.Selectable(mountString, mount == currentMountId))
                    {
                        currentMountId = mount;
                    }

                    DrawCurrentMountFightsTooltip(config.Mounts, mount);
                }
                ImGui.Unindent(15);
            }
        }
    }
}
