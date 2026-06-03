using System.IO;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.Commands.UploadImage;

public record UploadImageCommand(EquipmentId EquipmentId, Stream Stream, string FileName) : ICommand<Result>;
