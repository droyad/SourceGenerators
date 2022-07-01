using Example.Domain;

var character = new Character("Bob", Class.Sourcerer, 100)
{
    LeftHand = new Equipment("Spoon", 40)
    {
        Enchantments = { new Enchantment("Healing") }
    },
    Inventory =
    {
        new Equipment("Carrot", 1),
        new Equipment("Lamborghini Countach LP5000 Quattrovalvole", 1000)
    }
};


var readOnly = character.ToReadOnly();

Console.ReadLine();

