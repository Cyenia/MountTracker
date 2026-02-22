using System;
using System.Linq;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using MountTracker.Services;

namespace MountTracker.Classes;

[Serializable]
public class Fight(uint contentId)
{
    public uint ContentId { get; init; } = contentId;
    
    [NonSerialized]
    private static readonly ExcelSheet<ContentFinderCondition>? ContentFinderConditionSheet = PluginServices.DataManager.GameData.GetExcelSheet<ContentFinderCondition>();

    public override string ToString()
    {
        if(ContentFinderConditionSheet == null) return $"Fight-ID {ContentId}";
        ContentFinderCondition? content = ContentFinderConditionSheet.FirstOrDefault(content => content.RowId == ContentId);
        
        return content?.Name.ToString() ?? $"Fight-ID {ContentId}";
    }
}
