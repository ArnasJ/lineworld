using LanguageExt;
using static GameConfig;
using static LanguageExt.Prelude;

World PlayTurn(World world) =>
    world
        .ReduceCooldowns()
        .SpawnNewHero()
        .AdvanceHeroes()
        .ApplyGoldRewards()
        .RemoveDeadHeroes()
        .ChangeTurnPlayer();

var program =
    Eff(() => {
        var newWorld = new World(
            new Castle(CastleHP, StartingGold),
            new Castle(CastleHP, StartingGold),
            Enumerable.Repeat(Option<Hero>.None, WorldSize).ToArr(),
            Player.Player1
        );
        
        return new WorldEnumerator(newWorld, PlayTurn);
    }
);

program
    .Map(result => result.Select(world => world.ToVisual()))
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