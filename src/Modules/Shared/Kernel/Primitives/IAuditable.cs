namespace LabViroMol.Modules.Shared.Kernel.Primitives;

public interface ICreationAuditable;

public interface IModificationAuditable;

public interface IDeletionAuditable;

public interface IFullAuditable : ICreationAuditable, IModificationAuditable, IDeletionAuditable;
