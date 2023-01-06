public record Castle(HP hp, Gold gold);

public record Gold(int value) {
    public static Gold operator +(Gold gold1, Gold gold2) => new Gold(gold1.value + gold2.value);
    public static Gold operator -(Gold gold1, Gold gold2) => new Gold(gold1.value - gold2.value);
    public static Gold Min(Gold gold1, Gold gold2) => gold1.value <= gold2.value ? gold1 : gold2;
}
