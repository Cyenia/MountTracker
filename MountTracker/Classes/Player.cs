using System.Collections.Generic;

namespace MountTracker.Classes;

public class Player(string name, string server)
{
    public string Name { get; init; } = name;
    
    public string Server { get; init; } = server;
    
    public readonly Dictionary<uint, bool> Mounts = [];
    
    public void AddMount(uint mountId)
    {
        Mounts.TryAdd(mountId, false);
    }
    
    public void AddMounts(List<uint> mounts)
    {
        foreach (var mountId in mounts) Mounts.TryAdd(mountId, false);
    }
    
    public void SetObtained(uint mountId, bool obtained)
    {
        if(Mounts.ContainsKey(mountId))
        {
            Mounts[mountId] = obtained;
        }
        else
        {
            Mounts.TryAdd(mountId, obtained);
        }
    }
    
    public bool IsObtained(uint mountId)
    {
        return Mounts.TryGetValue(mountId, out var obtained) && obtained;
    }

    public override string ToString()
    {
        return $"{Name}@{Server}";
    }
}
