namespace bGamesPointsMod.Models;

public class LevelUpModel
{
    public int Experience { get; set; }
    public string Description { get; set; }

    public LevelUpModel(int experience, string description)
    {
        Experience = experience;
        Description = description;
    }
}