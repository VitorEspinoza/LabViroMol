using LabViroMol.Modules.Inventory.Application.Shared;
using LabViroMol.Modules.Inventory.Domain.Materials;
using LabViroMol.Modules.Inventory.Domain.References;
using LabViroMol.Modules.Research.Contracts;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Inventory.Application.Materials.Commands.ConsumeForProject;

public class ConsumeMaterialForProjectCommandHandler : ICommandHandler<ConsumeMaterialForProjectCommand, Result>
{
    private readonly IMaterialRepository _repository;
    private readonly IProjectChecker _projectChecker;
    private readonly ICurrentUser _currentUser;
    private readonly IInventoryUnitOfWork _unitOfWork;

    public ConsumeMaterialForProjectCommandHandler(
        IMaterialRepository repository,
        ICurrentUser currentUser,
        IInventoryUnitOfWork unitOfWork, IProjectChecker projectChecker)
    {
        _repository = repository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _projectChecker = projectChecker;
    }

    public async ValueTask<Result> Handle(ConsumeMaterialForProjectCommand command, CancellationToken ct)
    {
        var material = await _repository.GetByIdAsync(MaterialId.From(command.MaterialId), ct);

        if (material is null)
            return Result.NotFound("Material não encontrado.");

        var isEligibleResult = await _projectChecker.IsEligibleForConsumptionAsync(ProjectId.From(command.ProjectId), _currentUser.Id, ct);
        if (isEligibleResult.IsFailure)
            return isEligibleResult;
        
        var result = material.ConsumeForProject(command.ProjectId, command.Quantity, _currentUser.Id);

        if (result.IsFailure)
            return result;

        await _unitOfWork.CompleteAsync(ct);
        return Result.Success();
    }
}
