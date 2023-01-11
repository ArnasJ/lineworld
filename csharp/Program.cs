using System.Collections.Immutable;
using LanguageExt;
using static HeroExts;
using static LanguageExt.Prelude;

Eff<World> PlayTurnEff(World world, Random rng) =>
    GetHeroByPrice(world.CurrentPlayerGold(), world.currentPlayer, rng)
        .Map(newHeroOpt => PlayTurn(world, newHeroOpt));


World PlayTurn(World world, Option<Hero> newHeroOpt) =>
    world.ReduceCooldowns()
        .SpawnNewHero(newHeroOpt)
        .AdvanceHeroes()
        .ApplyGoldRewards()
        .RemoveDeadHeroes()
        .ChangeTurnPlayer();

Eff<ImmutableList<World>> PlayGame(World world, Random rng) {
    return rec(world, ImmutableList<World>.Empty.Add(world));

    Eff<ImmutableList<World>> rec(World current, ImmutableList<World> previous) =>
        PlayTurnEff(current, rng).Bind(
            newWorld => {
                var newWorlds = previous.Add(newWorld);
                return newWorld.IsGameFinished() ? SuccessEff(newWorlds) : rec(newWorld, newWorlds);
            }
        );
}

var program =
    from rng in Eff(() => new Random())
    let startingWorld = World.Create()
    from newWorld in PlayGame(startingWorld, rng)
    select newWorld;

program
    .Map(world => world.Select(w => w.ToVisual()))
    .Bind(WriteLineEnumerableEff)
    .Run()
    .Match(
        Succ: _ => { },
        Fail: error => {
            Console.Error.WriteLine(error);
            Environment.Exit(1);
        }
    );

Eff<Unit> WriteLineEnumerableEff(IEnumerable<string> str) =>
    Eff(() => {
        foreach (var s1 in str) {
            Console.WriteLine(s1); 
        }
        
        return Unit.Default;
    });