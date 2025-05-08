public class Transition : ITransition
{
    public Istate To { get; }
    public IPredicate Condition { get; }

    public Transition(Istate to, IPredicate condition)
    {
        To = to;
        Condition = condition;
    }
}