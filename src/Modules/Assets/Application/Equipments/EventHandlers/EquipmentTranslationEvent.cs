using LabViroMol.Modules.Assets.Domain.Equipments;
using Mediator;

namespace LabViroMol.Modules.Assets.Application.Equipments.EventHandlers;

public sealed record EquipmentTranslationEvent(EquipmentId EquipmentId) : INotification;