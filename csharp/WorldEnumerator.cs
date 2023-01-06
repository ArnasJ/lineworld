using System.Collections;

public record WorldEnumerator(World world, Func<World, World> create) : IEnumerable<World> {
    
    public IEnumerator<World> GetEnumerator() {
        var current = world;
        
        while (current.castle1.hp.value > 0 && current.castle2.hp.value > 0) {
            current = create(current);
            yield return current;
        }
    }

    public override string ToString() {
        return world.field.ToString();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}