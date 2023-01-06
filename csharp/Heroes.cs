﻿using ExhaustiveMatching;
using LanguageExt;
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
    
    public static Option<Hero> GetHeroByPrice(Gold goldToSpend, Player player) =>
        goldToSpend.value switch {
            var cost when cost >= ClericCost.value =>
                new Cleric(player, ClericCost, ClericDamage, ClericHP, ClericRange, new Cooldown(0)),
            var cost when cost >= ArcherCost.value =>
                new Archer(player, ArcherCost, ArcherDamage, ArcherHP, ArcherRange),
            var cost when cost >= WarriorCost.value =>
                new Warrior(player, WarriorCost, WarriorDamage, WarriorHP, WarriorRange),
            _ => Option<Hero>.None
        };
    
    public static Gold GetGoldReward(this Hero hero) =>
        new Gold((int)Math.Floor(hero.cost.value * KillGoldPercentage));
}