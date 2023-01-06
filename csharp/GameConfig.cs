public static class GameConfig {
    public static readonly HP CastleHP = new HP(100);
    public static readonly Gold StartingGold = new Gold(20);
    public static readonly Gold GoldPerTurn = new Gold(10);
    public static readonly Gold GoldLimit = new Gold(100);
    public static readonly float KillGoldPercentage = 0.2f;

    public static int WorldSize = 30;
    public static HP MaxUnitHP = new HP(9);

    public static readonly Gold WarriorCost = new Gold(15);
    public static readonly Damage WarriorDamage = new Damage(4);
    public static readonly HP WarriorHP = new HP(9);
    public static readonly Range WarriorRange = new Range(1);

    public static readonly Gold ArcherCost = new Gold(20);
    public static readonly Damage ArcherDamage = new Damage(2);
    public static readonly HP ArcherHP = new HP(5);
    public static readonly Range ArcherRange = new Range(4);

    public static readonly Gold ClericCost = new Gold(30);
    public static readonly Damage ClericDamage = new Damage(1);
    public static readonly HP ClericHP = new HP(3);
    public static readonly Range ClericRange = new Range(6);
    public static readonly Cooldown HealingCooldown = new Cooldown(3);
    public static readonly HP HealAmount = new HP(3);
}