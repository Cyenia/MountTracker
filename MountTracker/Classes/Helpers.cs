using System.Collections.Generic;
using Dalamud.Bindings.ImGui;

namespace MountTracker.Classes;

public static class Helpers
{
    public static void DrawCurrentMountFightsTooltip(OrderedDictionary<uint, Classes.Mount> mounts, uint currentMountId)
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
}
