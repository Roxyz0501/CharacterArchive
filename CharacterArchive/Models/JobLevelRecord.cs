namespace CharacterArchive.Models;

public sealed class JobLevelRecord
{
    public uint RowId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public short Level { get; set; }
}
