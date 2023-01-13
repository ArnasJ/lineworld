using System.Text;
using LanguageExt;
using LanguageExt.SomeHelp;
using static GameConfig;

/// <summary>
/// Represents the game world, including the state of the two castles, the field of heroes, and the current player.
/// </summary>
/// <param name="castle1">State of Player1 castle.</param>
/// <param name="castle2">State of Player2 castle.</param>
/// <param name="field">Array of options representing heroes for both players and empty spaces.</param>
/// <param name="currentPlayer">The player who is currently taking their turn.</param>
public record World(Castle castle1, Castle castle2, Arr<Option<Hero>> field, Player currentPlayer) {
    public static World Create() =>
        new World(
            new Castle(CastleHP, StartingGold),
            new Castle(CastleHP, StartingGold),
            Enumerable.Repeat(Option<Hero>.None, WorldSize).ToArr(),
            Player.Player1
        );

    /// <summary>
    /// Iterates the field from right to left and performs an action with each hero of the current player.
    /// If current player is Player2, field is first reversed before iterating and reversed after iteration is done.
    /// Hero's action is determined by the type and state of the hero.
    /// </summary>
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

    /// <summary>
    /// If cleric if able to heal a friendly hero, he does.
    /// Cleric's healing cooldown is refreshed after healing.
    /// Healed hero's HP is capped at <see cref="GameConfig.MaxHeroHP"/>.
    /// If cleric is not able to heal, <see cref="AttackOrMove"/> is called.
    /// </summary>
    public World HealOrAttackOrMove(Cleric cleric, int clericIdx) =>
        FindLowestHpFriendlyHeroIndex(clericIdx)
            .Match(
                heroToHealIdx => field.ElementAtOrDefault(heroToHealIdx).Match(
                    heroToHeal => {
                        var newHp = new HP(Math.Min(heroToHeal.hp.value + HealAmount.value, MaxHeroHP.value));
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

    /// <summary>
    /// Executes only the first available action for the given hero in the following order:
    /// <list type="number">
    /// <item> Attack enemy castle </item>
    /// <item> Attack enemy hero </item>
    /// <item> <see cref="MoveOrDoNothing"/> </item>
    /// </list>
    /// </summary>
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

    /// <summary>
    /// Determines if current attacker is within range to attack the enemy castle.
    /// Only checks for the castle to the right of the field. 
    /// </summary>
    public bool CanAttackCastle(Range attackRange, int attackerIdx) =>
        attackRange.value + attackerIdx >= field.Length;

    /// <summary>
    /// Tries to attack lowest HP enemy hero within the range of the attacker.
    /// Only checks for enemy heroes to the right of the attacker.
    /// </summary>
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

    /// <summary>
    /// Damages enemy castle with the amount given.
    /// </summary>
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

    /// <summary>
    /// Moves hero at current index to the right by one.
    /// Hero does not move if blocked by an enemy hero or is the last entry in the field.
    /// Hero will swap places with a friendly in front, if friendly has lower HP.
    /// </summary>
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

    /// <summary>
    /// Applies gold rewards to the current player.
    /// Rewards include gold for dead enemy heroes and gold bonus for a new turn.
    /// Player's gold is capped at <see cref="GameConfig.GoldLimit"/>.
    /// </summary>
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

    /// <summary>
    /// Removes the enemy heroes that are dead (hp less than or equal to 0) from the field.
    /// Keeps the field length the same.
    /// </summary>
    public World RemoveDeadHeroes() {
        var updatedField = field
            .Map(heroOpt =>
                heroOpt.Exists(h => h.player != currentPlayer && h.hp.value <= 0)
                    ? Option<Hero>.None
                    : heroOpt
            );

        return this with { field = updatedField };
    }

    /// <summary>
    /// Reduces the healing cooldown of all clerics belonging to the current player by 1.
    /// Minimum cooldown is capped at 0.
    /// </summary>
    public World ReduceHealingCooldown() {
        var updatedField = field
            .Map(heroOpt => heroOpt.Match(
                hero => hero.player == currentPlayer
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

    /// <summary>
    /// Adds new hero to the field and subtracts the respective cost from current player.
    /// Performs no changes if a new hero cannot be placed.
    /// </summary>
    public World BuyNewHero(Hero newHero) {
        var castle = currentPlayer == Player.Player1 ? castle1 : castle2;

        return AddHeroToField(newHero)
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
            );
    }

    /// <summary>
    /// Finds the index of a friendly hero with the lowest HP.
    /// Excludes the current hero.
    /// </summary>
    /// <returns>Some if found, None if not found.</returns>
    public Option<int> FindLowestHpFriendlyHeroIndex(int currentHeroIdx) =>
        field
            .Select((opt, i) => (opt, i))
            .Where(tpl => tpl.opt.Match(h => h.player == currentPlayer && tpl.i != currentHeroIdx, () => false))
            .Bind(tpl => tpl.opt)
            .OrderBy(h => h.hp.value)
            .HeadOrNone()
            .Bind(h => field.IndexOfOpt(h));

    /// <summary>
    /// Finds the index of an enemy hero with the lowest HP that is in range of the attacker.
    /// Only searches for heroes to the right in the field.
    /// </summary>
    /// <returns>Some if found, None if not.</returns>
    public Option<int> FindLowestHpEnemyIndex(Range attackRange, int attackerIdx) =>
        field
            .Skip(attackerIdx + 1)
            .Take(attackRange.value)
            .SelectMany(x => x)
            .Where(h => h.player != currentPlayer)
            .OrderBy(h => h.hp.value)
            .HeadOrNone()
            .Bind(h => field.IndexOfOpt(h));

    /// <summary>
    /// Tries to place a new hero in the field, following these rules:
    /// <list type="bullet">
    /// <item> Field keeps the same length after spawning. </item>
    /// <item> Hero for Player1 is spawned at the beginning of the field. </item>
    /// <item> Hero for Player2 is spawned at the end of the field. </item>
    /// <item> If spawn place is blocked by friendly hero, all blocking heroes are pushed towards the enemy by 1. </item>
    /// <item> If there is no free space until the first enemy hero, a new hero will not be spawned. </item>
    /// </list>
    /// </summary>
    /// <returns>Some if spawned, None if not.</returns>
    private Option<Arr<Option<Hero>>> AddHeroToField(Hero newHero) {
        var linedUpField = currentPlayer == Player.Player1 ? field : field.Reverse();
        
        var fieldUntilFirstEnemy = linedUpField
            .TakeWhile(heroOpt => heroOpt.Match(
                hero => hero.player == currentPlayer,
                () => true
            ));
    
        if (fieldUntilFirstEnemy.All(h => h.IsSome))
            return Option<Arr<Option<Hero>>>.None;

        var fieldWithNewHero = linedUpField
            .Remove(Option<Hero>.None)
            .Prepend(newHero.ToSome())
            .ToArr();

        return currentPlayer == Player.Player1 ? fieldWithNewHero : fieldWithNewHero.Reverse();
    }

    public Gold CurrentPlayerGold() =>
        currentPlayer == Player.Player1
            ? castle1.gold
            : castle2.gold;

    /// <summary>
    /// The game is finished when the hp of either castle 1 or castle 2 is less than or equal to 0.
    /// </summary>
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