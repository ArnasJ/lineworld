using System.Text;
using LanguageExt;
using LanguageExt.SomeHelp;
using static GameConfig;

public record World(Castle castle1, Castle castle2, Arr<Option<Hero>> field, Player currentPlayer) {
    public static World Create() =>
        new World(
            new Castle(CastleHP, StartingGold),
            new Castle(CastleHP, StartingGold),
            Enumerable.Repeat(Option<Hero>.None, WorldSize).ToArr(),
            Player.Player1
        );

    public World AdvanceHeroes() {
        var linedUpWorld = currentPlayer == Player.Player1 ? this : this with { field = field.Reverse() };

        var updatedWorld = linedUpWorld.field
            .Select((heroOpt, i) => (heroOpt, i))
            .FoldBack(linedUpWorld, (currentWorld, tpl) =>
                currentWorld.field[tpl.i].Match(
                    hero => hero.player == currentPlayer
                        ? hero switch {
                            Cleric cleric => cleric.cooldown.turns switch {
                                0 => currentWorld.HealOrAttackOrMove(cleric, tpl.i),
                                _ => currentWorld.AttackOrMove(cleric, tpl.i)
                            },
                            _ => currentWorld.AttackOrMove(hero, tpl.i)
                        }
                        : currentWorld,
                    () => currentWorld
                )
            );

        return updatedWorld.currentPlayer == Player.Player1
            ? updatedWorld
            : updatedWorld with { field = updatedWorld.field.Reverse() };
    }

    public World HealOrAttackOrMove(Cleric cleric, int clericIdx) =>
        FindLowestHpFriendlyHeroIndex(clericIdx)
            .Match(
                heroToHealIdx => field.ElementAtOrDefault(heroToHealIdx).Match(
                    heroToHeal => {
                        var newHp = new HP(Math.Min(heroToHeal.hp.value + HealAmount.value, MaxUnitHP.value));
                        var healedHero = heroToHeal with { hp = newHp };
                        var clericWithCd = cleric with { cooldown = HealingCooldown };
                        var updatedField = field
                            .SetItem(heroToHealIdx, healedHero.ToSome())
                            .SetItem(clericIdx, clericWithCd.ToSome<Hero>());

                        return this with { field = updatedField };
                    },
                    () => throw new Exception("Impossible!")
                ),
                () => AttackOrMove(cleric, clericIdx)
            );

    private World AttackOrMove(Hero attacker, int attackerIdx) {
        if (CanAttackCastle(attacker.range, attackerIdx)) {
            return DoCastleDamage(attacker.damage);
        }

        return TryAttack(attacker, attackerIdx)
            .Match(
                updatedWorld => updatedWorld,
                () => MoveOrDoNothing(attacker, attackerIdx)
            );
    }

    public bool CanAttackCastle(Range attackRange, int attackerIdx) =>
        attackRange.value + attackerIdx >= field.Length;

    public Option<World> TryAttack(Hero attacker, int attackerIdx) =>
        FindLowestHpEnemyIndex(attacker.range, attackerIdx).Match(
            defenderIdx => field.ElementAtOrDefault(defenderIdx).Match(
                defender => this with {
                    field = field.SetItem(
                        defenderIdx, defender with { hp = defender.hp.DoDamage(attacker.damage) }
                    )
                },
                () => throw new Exception("Impossible!")
            ),
            () => Option<World>.None
        );

    public World DoCastleDamage(Damage damage) {
        var p1 =
            currentPlayer == Player.Player1
                ? castle1
                : castle1 with { hp = new HP(castle1.hp.value - damage.value) };
    
        var p2 =
            currentPlayer == Player.Player2
                ? castle2
                : castle2 with { hp = new HP(castle2.hp.value - damage.value) };

        return this with { castle1 = p1, castle2 = p2 };
    }

    public World MoveOrDoNothing(Hero hero, int heroIdx) {
        if (heroIdx == field.Length - 1) {
            return this;
        }

        return field[heroIdx + 1].Match(
            nextHero => {
                if (nextHero.player != hero.player) {
                    return this;
                }

                if (nextHero.hp.value < hero.hp.value) {
                    return this with { field = field.SwapElements(heroIdx, heroIdx + 1)};
                }

                return this;
            },
            () => this with { field = field.SwapElements(heroIdx, heroIdx + 1) }
        );
    }

    public World ApplyGoldRewards() {
        var goldForKills =
            field.Fold(
                new Gold(0), (currentGold, heroOpt) => heroOpt.Match(
                    hero => {
                        var goldReward = hero.player != currentPlayer && hero.hp.value <= 0
                            ? hero.GetGoldReward()
                            : new Gold(0);

                        return currentGold + goldReward;
                    },
                    currentGold
                )
            );
    
        var p1 = currentPlayer == Player.Player1
            ? castle1 with { gold = Gold.Min(GoldLimit, castle1.gold + goldForKills + GoldPerTurn) }
            : castle1;

        var p2 = currentPlayer == Player.Player2
            ? castle2 with { gold = Gold.Min(GoldLimit, castle2.gold + goldForKills + GoldPerTurn) }
            : castle2;

        return this with { castle1 = p1, castle2 = p2 };
    }

    public World RemoveDeadHeroes() {
        var updatedField = field
            .Map(heroOpt =>
                heroOpt.Exists(h => h.player != currentPlayer && h.hp.value <= 0)
                    ? Option<Hero>.None
                    : heroOpt
            );

        return this with { field = updatedField };
    }

    public World ReduceCooldowns() {
        var updatedField = field
            .Map(heroOpt => heroOpt.Match(
                hero => hero.player == Player.Player1
                    ? hero switch {
                        Cleric cleric => cleric with { cooldown = new Cooldown(Math.Max(0, cleric.cooldown.turns - 1)) },
                        _ => heroOpt
                    }
                    : heroOpt,
                () => heroOpt
            ));
    
        return this with { field = updatedField };
    }

    public World ChangeTurnPlayer() {
        var opponent = currentPlayer == Player.Player1 ? Player.Player2 : Player.Player1;
        return this with { currentPlayer = opponent };
    }

    public World SpawnNewHero(Option<Hero> newHeroOpt) {
        var castle = currentPlayer == Player.Player1 ? castle1 : castle2;

        return newHeroOpt.Match(
            newHero => AddHeroToField(newHero, currentPlayer)
                .Match(
                    fieldWithNewHero => {
                        var newCastle = castle with { gold = castle.gold - newHero.cost };
                        var p1Castle = currentPlayer == Player.Player1 ? newCastle : castle1;
                        var p2Castle = currentPlayer == Player.Player2 ? newCastle : castle2;

                        return currentPlayer == Player.Player1
                            ? this with { castle1 = p1Castle, field = fieldWithNewHero }
                            : this with { castle2 = p2Castle, field = fieldWithNewHero };
                    },
                    () => this
                ),
            () => this
        );
    }

    public Option<int> FindLowestHpFriendlyHeroIndex(int currentHeroIdx) =>
        field
            .Select((opt, i) => (opt, i))
            .Where(tpl => tpl.opt.Match(h => h.player == currentPlayer && tpl.i != currentHeroIdx, () => false))
            .Bind(tpl => tpl.opt)
            .OrderBy(h => h.hp.value)
            .HeadOrNone()
            .Bind(h => field.IndexOfOpt(h));

    public Option<int> FindLowestHpEnemyIndex(Range attackRange, int attackerIdx) =>
        field
            .Skip(attackerIdx + 1)
            .Take(attackRange.value)
            .SelectMany(x => x)
            .Where(h => h.player != currentPlayer)
            .OrderBy(h => h.hp.value)
            .HeadOrNone()
            .Bind(h => field.IndexOfOpt(h));

    private Option<Arr<Option<Hero>>> AddHeroToField(Hero newHero, Player player) {
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

    public Gold CurrentPlayerGold() =>
        currentPlayer == Player.Player1
            ? castle1.gold
            : castle2.gold;

    public bool IsGameFinished() =>
        castle1.hp.value <= 0 || castle2.hp.value <= 0;

    public string ToVisual() {
        var sb = new StringBuilder();
        sb.Append(string.Concat(field.Map(heroOpt => heroOpt.Match(h => h.GetIndicator(), () => '.'))));

        if (IsGameFinished()) {
            var result = $"Game End: Player1: {castle1.hp.value}, Player2: {castle2.hp.value}";
            sb.Append('\n');
            sb.Append(result);
        }
        
        return sb.ToString();
    }
}