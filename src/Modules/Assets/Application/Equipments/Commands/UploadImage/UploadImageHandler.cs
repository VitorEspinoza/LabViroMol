using LabViroMol.Modules.Assets.Application.Shared;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.UploadImage;

public class UploadImageHandler(
    IImageStorageService storage,
    IEquipmentRepository equipmentRepository,
    IAssetsUnitOfWork unitOfWork) : ICommandHandler<UploadImageCommand, Result>
{
    public async ValueTask<Result> Handle(UploadImageCommand command, CancellationToken ct)
    {
        var equipment = await equipmentRepository.GetByIdAsync(command.EquipmentId, ct);

        if (equipment is null)
            return Result.NotFound("Equipamento não encontrado");

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
            await storage.SaveEquipmentImageAsync(
                equipment.Id.Value,
                command.Stream,
                extension,
                ct);

        equipment.AttachImageUrl(imageUrl);

        await unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}
