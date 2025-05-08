public interface ITransition
{
    Istate To { get; }
    IPredicate Condition { get; }
}