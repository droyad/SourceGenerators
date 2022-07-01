using System.Diagnostics.Contracts;

namespace Example.Domain;

public enum Class
{
    Sourcerer,
    Warrior,
    Paladin
}

public partial class Character
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

    [Pure]
    public List<Equipment> GetEquipmentWithWearLessThan(int wear)
    {
        // var result = Inventory.Count(i => i.WearLeft < wear);
        // if (LeftHand != null && LeftHand.WearLeft < wear)
        //     result++;
        // if (RightHand != null && RightHand.WearLeft < wear)
        //     result++;
        //
        // return result;

        var result = Inventory.Where(i => i.WearLeft < wear).ToList();
        if (LeftHand != null && LeftHand.WearLeft < wear)
            result.Add(LeftHand);
        if (RightHand != null && RightHand.WearLeft < wear)
            result.Add(RightHand);

        return result;
    }
}

public partial class Equipment
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