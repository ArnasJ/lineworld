using LanguageExt;
using LanguageExt.SomeHelp;


public static class ArrayExts {
    public static Option<int> IndexOfOpt<A>(this Arr<A> arr, A a) =>
        arr.IndexOf(a) switch {
            -1 => Option<int>.None,
            var i => i.ToSome()
        };

    public static Arr<A> SwapElements<A>(this Arr<A> arr, int index1, int index2) {
        var item1 = arr[index1];
        var item2 = arr[index2];

        return arr.SetItem(index1, item2).SetItem(index2, item1);
    }
}