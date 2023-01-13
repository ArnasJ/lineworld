using ExhaustiveMatching;
using LanguageExt;
using static LanguageExt.Prelude;
using static GameConfig;

[Closed(typeof(Warrior), typeof(Archer), typeof(Cleric))]
public abstract record Hero(Player player, Gold cost, Damage damage, HP hp, Range range);

public record Warrior(Player player, Gold cost, Damage damage, HP hp, Range range)
    : Hero(player, cost, damage, hp, range);

public record Archer(Player player, Gold cost, Damage damage, HP hp, Range range)
    : Hero(player, cost, damage, hp, range);

public record Cleric(Player player, Gold cost, Damage damage, HP hp, Range range, Cooldown cooldown)
    : Hero(player, cost, damage, hp, range);

public enum Player { Player1, Player2 }
public record Damage(int value);
public record HP(int value) {
    public static HP operator +(HP hp1, HP hp2) => new HP(hp1.value + hp2.value);
    public static HP operator -(HP hp1, HP hp2) => new HP(hp1.value - hp2.value);
    public HP DoDamage(Damage damage) => new HP(value - damage.value);
}
public record Range(int value);
public record Cooldown(int turns);


public static class HeroExts {
    public static char GetIndicator(this Hero hero) =>
        hero switch {
            Archer archer => archer.player == Player.Player1 ? '}' : '{',
            Cleric cleric => cleric.player == Player.Player1 ? ')' : '(',
            Warrior warrior => warrior.player == Player.Player1 ? ']' : '[',
            _ => throw ExhaustiveMatch.Failed(hero)
        };

    /// <summary>
    /// Randomly generates a new hero for the given player, if hero cost is below <see cref="goldToSpend"/>.
    /// </summary>
    /// <returns>Some if generated new hero, None if not.</returns>
    public static Eff<Option<Hero>> GetHeroByPrice(Gold goldToSpend, Player player, Random rng) =>
        Eff(() => 
            rng.Next(4) switch {
                0 when goldToSpend.value >= ClericCost.value => 
                    new Cleric(player, ClericCost, ClericDamage, ClericHP, ClericRange, new Cooldown(0)),
                1 when goldToSpend.value >= ArcherCost.value => 
                    new Archer(player, ArcherCost, ArcherDamage, ArcherHP, ArcherRange),
                2 when goldToSpend.value >= WarriorCost.value => 
                    new Warrior(player, WarriorCost, WarriorDamage, WarriorHP, WarriorRange),
                _ => Option<Hero>.None,
            }
        );

    public static Gold GetGoldReward(this Hero hero) =>
        new Gold((int)Math.Floor(hero.cost.value * KillGoldPercentage));
}