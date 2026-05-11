using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Abstractions.Interfaces;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.UploadImage;

public class UploadImageHandler : ICommandHandler<UploadImageCommand, Result>
{
    private readonly IImageStorageService _storage;
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly ICurrentUser _currentUser;
    private readonly IAssetsUnitOfWork _unitOfWork;

    public UploadImageHandler(
        IImageStorageService storage,
        IEquipmentRepository equipmentRepository,
        ICurrentUser currentUser,
        IAssetsUnitOfWork unitOfWork)
    {
        _storage = storage;
        _equipmentRepository = equipmentRepository;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }
    
    public async ValueTask<Result> Handle(UploadImageCommand command, CancellationToken ct)
    {
        var equipment = await _equipmentRepository.GetByIdAsync(command.EquipmentId, ct);

        if (equipment is null)
            Result.NotFound("Equipamento não encontrado");
        
        var extension =
            Path.GetExtension(command.FileName);
        
        var allowedExtensions = new[]
        {
            ".png",
            ".jpg",
            ".jpeg"
        };
        
        if (!allowedExtensions.Contains(
                extension.ToLowerInvariant()))
        {
            return Result.BusinessRule(
                "Invalid image extension");
        }
        
        var imageUrl =
            await _storage.SaveEquipmentImageAsync(
                equipment!.Id.Value,
                command.Stream,
                extension,
                ct);
        
        equipment.AttachImageUrl(imageUrl);
        equipment.MarkAsUpdated(_currentUser.Id);

        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}