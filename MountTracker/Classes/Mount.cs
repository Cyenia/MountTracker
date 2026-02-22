using System;
using System.Collections.Generic;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using MountTracker.Services;

namespace MountTracker.Classes;

[Serializable]
public class Mount(uint mountId)
{
    public uint MountId { get; init; } = mountId;
    public readonly OrderedDictionary<uint, Fight> Fights = [];

    [NonSerialized]
    private static readonly ExcelSheet<Item>? ItemSheet = PluginServices.DataManager.GameData.GetExcelSheet<Item>();

    public OrderedDictionary<uint, Fight> GetFights()
    {
        return Fights;
    }
    
    public void AddFight(Fight fight)
    {
        Fights.TryAdd(fight.ContentId, fight);
    }
    
    public override string ToString()
    {
        if(ItemSheet == null) return $"Item-ID {MountId}";
        Item? item = ItemSheet.FirstOrDefault(item => item.RowId == MountId);
        
        return item?.Name.ToString() ?? $"Item-ID {MountId}";
    }
}
