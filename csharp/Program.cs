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

Eff<World> PlayGame(World world, Random rng) =>
    PlayTurnEff(world, rng)
        .Bind(newWorld =>
            newWorld.IsGameFinished()
                ? SuccessEff(newWorld)
                : PlayGame(newWorld, rng)
        );

var program =
    from rng in Eff(() => new Random())
    let startingWorld = WorldExts.Create()
    from newWorld in PlayGame(startingWorld, rng)
    select newWorld;

program
    .Map(world => world.ToVisual())
    .Bind(WriteLineEff)
    .Run()
    .Match(
        Succ: _ => { },
        Fail: error => {
            Console.Error.WriteLine(error);
            Environment.Exit(1);
        }
    );

Eff<Unit> WriteLineEff(string str) =>
    Eff(() => {
        Console.WriteLine(str);
        return Unit.Default;
    });