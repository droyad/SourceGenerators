namespace Example.Domain;

public enum Class
{
    Sourcerer,
    Warrior,
    Paladin
}

public class Character
{
    public Character(string name, Class @class, int hitPoints)
    {
        Name = name;
        Class = @class;
        HitPoints = hitPoints;
    }

    public string Name { get; }
    public Class Class { get; }
    public int HitPoints { get; set; }
    public Equipment? LeftHand { get; set; }
    public Equipment? RightHand { get; set; }
    public List<Equipment> Inventory { get; } = new();
}

public class Equipment
{
    public Equipment(string name, int wearLeft)
    {
        Name = name;
        WearLeft = wearLeft;
    }

    public string Name { get; }
    public int WearLeft { get; set; }

    public List<Enchantment> Enchantments { get; } = new();
}

public record Enchantment(string Name);