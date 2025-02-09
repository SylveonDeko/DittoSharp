namespace Ditto.Database.Models.PostgreSQL.Pokemon;

public class Egg
{
    public ulong UserId { get; set; }
    
    // Basic Pokemon Eggs
    public int Bidoof { get; set; }
    public int Caterpie { get; set; }
    public int Pidgey { get; set; }
    public int Magikarp { get; set; }
    public int Spinarak { get; set; }
    public int Tentacruel { get; set; }
    public int Togepi { get; set; }
    public int Bellsprout { get; set; }
    
    // Rare Pokemon Eggs
    public int Chansey { get; set; }
    public int Omastar { get; set; }
    public int Cubone { get; set; }
    public int Farfetchd { get; set; }
    public int Porygon { get; set; }
    public int Ralts { get; set; }
    
    // Dragon Pokemon Eggs
    public int Dratini { get; set; }
    public int Larvitar { get; set; }
    public int Bagon { get; set; }
    public int Gible { get; set; }
    
    // Legendary Pokemon Eggs
    public int Kyogre { get; set; }
    public int Dialga { get; set; }
    
    // Special Flags
    public bool GotRadiant { get; set; }
}