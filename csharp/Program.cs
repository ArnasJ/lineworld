using LanguageExt;
using static LanguageExt.Prelude;

World PlayTurn(World world, Random rng) =>
    world
        .ReduceCooldowns()
        .SpawnNewHero(rng)
        .AdvanceHeroes()
        .ApplyGoldRewards()
        .RemoveDeadHeroes()
        .ChangeTurnPlayer();

var program =
    from rng in Eff(() => new Random())
    let newWorld = WorldExts.Create()
    select new WorldEnumerator(newWorld, w => PlayTurn(w, rng));

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