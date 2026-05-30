namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public interface IStrongId<TSelf> where TSelf : struct, IStrongId<TSelf>
{
    Guid Value { get; }
    static abstract TSelf From(Guid value);
}

public static class IdFactory
{
    public static TId New<TId>() where TId : struct, IStrongId<TId>
    {
        return TId.From(Guid.CreateVersion7());
    }
}