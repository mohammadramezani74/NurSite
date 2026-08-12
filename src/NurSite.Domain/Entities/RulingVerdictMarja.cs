namespace NurSite.Domain.Entities;

public class RulingVerdictMarja
{
    public int RulingVerdictId { get; set; }
    public RulingVerdict RulingVerdict { get; set; } = default!;

    public int MarjaId { get; set; }
    public Marja Marja { get; set; } = default!;
}