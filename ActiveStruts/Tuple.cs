namespace ActiveStruts
{
    public class Tuple<T1, T2>
    {
        public T1 Item1 { get; private set; }
        public T2 Item2 { get; private set; }

        internal Tuple(T1 first, T2 second)
        {
            this.Item1 = first;
            this.Item2 = second;
        }
    }

    public class Tuple<T1, T2, T3> : ASUtil.Tuple<T1, T2>
    {
        public T3 Item3 { get; private set; }

        internal Tuple(T1 first, T2 second, T3 third)
            : base(first, second)
        {
            this.Item3 = third;
        }
    }

    public static class Tuple
    {
        public static Tuple<T1, T2> New<T1, T2>(T1 first, T2 second)
        {
            var tuple = new Tuple<T1, T2>(first, second);
            return tuple;
        }

        public static Tuple<T1, T2, T3> New<T1, T2, T3>(T1 first, T2 second, T3 third)
        {
            var tuple = new Tuple<T1, T2, T3>(first, second, third);
            return tuple;
        }
    }
}