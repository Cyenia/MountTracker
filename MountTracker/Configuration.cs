using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using MountTracker.Services;

namespace MountTracker;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public SortedSet<uint> TrackedMounts { get; set; } = [];
    public List<Classes.Player> Player { get; set; } = [];
    public OrderedDictionary<uint, Classes.Mount> Mounts { get; set; } = [];

    public void Save()
    {
        PluginServices.PluginInterface.SavePluginConfig(this);
    }
}
