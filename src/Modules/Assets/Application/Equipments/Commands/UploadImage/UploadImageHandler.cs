using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Infrastructure.Storage;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.Extensions.Options;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.UploadImage;

public class UploadImageHandler : ICommandHandler<UploadImageCommand, Result>
{
    private readonly IEquipmentRepository _equipmentRepository;
    private readonly IAssetsUnitOfWork _unitOfWork;
    private readonly IFileStorage _storage;
    private readonly StorageSettings _storageSettings;

    public UploadImageHandler(
        IEquipmentRepository equipmentRepository,
        IAssetsUnitOfWork unitOfWork,
        IFileStorage storage,
        IOptions<StorageSettings> storageSettings)
    {
        _equipmentRepository = equipmentRepository;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _storageSettings = storageSettings.Value;
    }
    
    public async ValueTask<Result> Handle(
        UploadImageCommand command,
        CancellationToken ct)
    {
        var equipment =
            await _equipmentRepository.GetByIdAsync(
                command.EquipmentId,
                ct);

        if (equipment is null)
            return Result.NotFound(
                "Equipamento não encontrado");

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

        var fileName =
            $"{equipment.Id.Value}{extension}";

        var imageUrl =
            await _storage.SaveAsync(
                command.Stream,
                fileName,
                _storageSettings.Folders.Equipments,
                ct);

        equipment.AttachImageUrl(imageUrl);

        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}