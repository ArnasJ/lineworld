using System.Text;
using LanguageExt;
using LanguageExt.SomeHelp;
using static GameConfig;

public record World(Castle castle1, Castle castle2, Arr<Option<Hero>> field, Player currentPlayer);

public static class WorldExts {
    public static World Create() =>
        new World(
            new Castle(CastleHP, StartingGold),
            new Castle(CastleHP, StartingGold),
            Enumerable.Repeat(Option<Hero>.None, WorldSize).ToArr(),
            Player.Player1
        );

    public static World AdvanceHeroes(this World world) {
        var linedUpWorld = world.currentPlayer == Player.Player1 ? world : world with { field = world.field.Reverse() };

        var updatedWorld = linedUpWorld.field
            .Select((_, i) => i)
            .FoldBack(linedUpWorld, (currentWorld, index) =>
                currentWorld.field[index].Match(
                    hero => hero.player == world.currentPlayer
                        ? hero switch {
                            Cleric cleric => cleric.cooldown.turns switch {
                                0 => currentWorld.HealOrAttackOrMove(cleric),
                                _ => currentWorld.AttackOrMove(cleric)
                            },
                            _ => currentWorld.AttackOrMove(hero)
                        }
                        : currentWorld,
                    () => currentWorld
                )
            );

        return updatedWorld.currentPlayer == Player.Player1
            ? updatedWorld
            : updatedWorld with { field = updatedWorld.field.Reverse() };
    }
    
    public static World HealOrAttackOrMove(this World world, Cleric cleric) =>
        FindLowestHpFriendlyHeroIndex(world.field, cleric)
            .Match(
                heroIdx => world.field.IndexOfOpt(cleric).Match(
                    clericIdx => world.field.ElementAtOrDefault(heroIdx).Match(
                        hero => {
                            var newHp = new HP(Math.Min(hero.hp.value + HealAmount.value, MaxUnitHP.value));
                            var healedHero = hero with { hp = newHp };
                            var clericWithCd = cleric with { cooldown = HealingCooldown };
                            var updatedField = world.field
                                .SetItem(heroIdx, healedHero.ToSome())
                                .SetItem(clericIdx, clericWithCd.ToSome<Hero>());

                            return world with { field = updatedField };
                        },
                        () => throw new Exception("Impossible!")
                    ),
                    () => throw new Exception("Cleric is not on the field")
                ),
                () => world.AttackOrMove(cleric));
    
    private static World AttackOrMove(this World world, Hero hero) {
        if (CanAttackCastle(world, hero)) {
            return DoCastleDamage(world, hero.damage);
        }

        return world.TryAttack(hero)
            .Match(
                updatedWorld => updatedWorld,
                () => world.MoveOrDoNothing(hero)
            );
    }

    public static bool CanAttackCastle(World world, Hero hero) =>
        world.field.IndexOfOpt(hero)
            .Match(
                idx => hero.range.value + idx >= world.field.Length,
                () => throw new Exception("Hero is not on field")
            );

    public static Option<World> TryAttack(this World world, Hero attacker) =>
        FindLowestHpEnemyIndex(world.field, attacker).Match(
            defenderIdx => world.field.ElementAtOrDefault(defenderIdx).Match(
                defender => world with {
                    field = world.field.SetItem(
                        defenderIdx, defender with { hp = defender.hp.DoDamage(attacker.damage) }
                    )
                },
                () => throw new Exception("Impossible!")
            ),
            () => Option<World>.None
        );
    
    public static World DoCastleDamage(this World world, Damage damage) {
        var castle1 =
            world.currentPlayer == Player.Player1
                ? world.castle1
                : world.castle1 with { hp = new HP(world.castle1.hp.value - damage.value) };
    
        var castle2 =
            world.currentPlayer == Player.Player2
                ? world.castle2
                : world.castle2 with { hp = new HP(world.castle2.hp.value - damage.value) };

        return world with { castle1 = castle1, castle2 = castle2 };
    }

    public static World MoveOrDoNothing(this World world, Hero hero) =>
        world.field.IndexOfOpt(hero).Match(
            heroIdx => {
                if (heroIdx == world.field.Length - 1) {
                    return world;
                }

                return world.field[heroIdx + 1].Match(
                    nextHero => {
                        if (nextHero.player != hero.player) {
                            return world;
                        }

                        if (nextHero.hp.value < hero.hp.value) {
                            return world with { field = world.field.SwapElements(heroIdx, heroIdx + 1)};
                        }

                        return world;
                    },
                    () => world with { field = world.field.SwapElements(heroIdx, heroIdx + 1) }
                );
            },
            () => throw new Exception("Hero is not on field")
        );
    
    public static World ApplyGoldRewards(this World world) {
        var goldForKills =
            world.field.Fold(
                new Gold(0), (currentGold, heroOpt) => heroOpt.Match(
                    hero => {
                        var goldReward = hero.player != world.currentPlayer && hero.hp.value <= 0
                            ? hero.GetGoldReward()
                            : new Gold(0);

                        return currentGold + goldReward;
                    },
                    currentGold
                )
            );
    
        var p1 = world.currentPlayer == Player.Player1
            ? world.castle1 with { gold = Gold.Min(GoldLimit, world.castle1.gold + goldForKills + GoldPerTurn) }
            : world.castle1;

        var p2 = world.currentPlayer == Player.Player2
            ? world.castle2 with { gold = Gold.Min(GoldLimit, world.castle2.gold + goldForKills + GoldPerTurn) }
            : world.castle2;

        return world with { castle1 = p1, castle2 = p2 };
    }
    
    public static World RemoveDeadHeroes(this World world) {
        var updatedField = world.field
            .Map(heroOpt =>
                heroOpt.Exists(h => h.player != world.currentPlayer && h.hp.value <= 0)
                    ? Option<Hero>.None
                    : heroOpt
            );

        return world with { field = updatedField };
    }

    public static World ReduceCooldowns(this World world) {
        var updatedField = world.field
            .Map(heroOpt => heroOpt.Match(
                hero => hero.player == Player.Player1
                    ? hero switch {
                        Cleric cleric => cleric with { cooldown = new Cooldown(cleric.cooldown.turns - 1) },
                        _ => heroOpt
                    }
                    : heroOpt,
                () => heroOpt
            ));
    
        return world with { field = updatedField };
    }

    public static World ChangeTurnPlayer(this World world) {
        var opponent = world.currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;
        return world with { currentPlayer = opponent };
    }
    
    public static World SpawnNewHero(this World world, Option<Hero> newHeroOpt) {
        var currentPlayer = world.currentPlayer;
        var castle = currentPlayer == Player.Player1 ? world.castle1 : world.castle2;

        return newHeroOpt.Match(
            newHero => AddHeroToField(world.field, newHero, currentPlayer)
                .Match(
                    fieldWithNewHero => {
                        var newCastle = castle with { gold = castle.gold - newHero.cost };
                        var p1Castle = currentPlayer == Player.Player1 ? newCastle : world.castle1;
                        var p2Castle = currentPlayer == Player.Player2 ? newCastle : world.castle2;

                        return currentPlayer == Player.Player1
                            ? world with { castle1 = p1Castle, field = fieldWithNewHero }
                            : world with { castle2 = p2Castle, field = fieldWithNewHero };
                    },
                    () => world
                ),
            () => world
        );
    }

    public static Option<int> FindLowestHpFriendlyHeroIndex(Arr<Option<Hero>> field, Hero currentHero) =>
        field
            .SelectMany(x => x)
            .Where(h => h.player == currentHero.player && !ReferenceEquals(h, currentHero))
            .OrderBy(h => h.hp.value)
            .HeadOrNone()
            .Map(h => field.IndexOfOpt(h))
            .Flatten();
    
    public static Option<int> FindLowestHpEnemyIndex(Arr<Option<Hero>> field, Hero attacker) =>
        field.IndexOfOpt(attacker)
            .Match(
                idx => field
                    .Skip(idx + 1)
                    .Take(attacker.range.value)
                    .SelectMany(x => x)
                    .Where(h => h.player != attacker.player)
                    .OrderBy(h => h.hp.value)
                    .HeadOrNone()
                    .Map(h => field.IndexOfOpt(h))
                    .Flatten(),
                () => Option<int>.None
            );

    private static Option<Arr<Option<Hero>>> AddHeroToField(Arr<Option<Hero>> field, Hero newHero, Player player) {
        var fieldUntilFirstEnemy = field
            .TakeWhile(heroOpt => heroOpt.Match(
                hero => hero.player == player,
                () => true
            ));
    
        if (fieldUntilFirstEnemy.All(h => h.IsSome))
            return Option<Arr<Option<Hero>>>.None;

        return player == Player.Player1
            ? field
                .Remove(Option<Hero>.None)
                .Prepend(newHero.ToSome())
                .ToArr()
                .ToSome()
            : field
                .Reverse()
                .Remove(Option<Hero>.None)
                .Reverse()
                .Append(newHero.ToSome())
                .ToArr()
                .ToSome();
    }

    public static Gold CurrentPlayerGold(this World world) =>
        world.currentPlayer == Player.Player1
            ? world.castle1.gold
            : world.castle2.gold;

    public static bool IsGameFinished(this World world) =>
        world.castle1.hp.value <= 0 || world.castle2.hp.value <= 0;
    
    public static string ToVisual(this World world) {
        var sb = new StringBuilder();
        sb.Append(string.Concat(world.field.Map(heroOpt => heroOpt.Match(h => h.GetIndicator(), () => '.'))));

        if (world.castle1.hp.value <= 0 || world.castle2.hp.value <= 0) {
            var result = $"Game End: Player1: {world.castle1.hp.value}, Player2: {world.castle2.hp.value}";
            sb.Append('\n');
            sb.Append(result);
        }
        
        return sb.ToString();
    }
}