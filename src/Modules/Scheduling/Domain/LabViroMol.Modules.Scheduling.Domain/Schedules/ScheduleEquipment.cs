namespace LabViroMol.Modules.Scheduling.Domain.Schedules;

public class ScheduleEquipment
{
    public Guid EquipmentId { get; private set; }
    public string Name { get; private set; }

    private ScheduleEquipment() { }

    public ScheduleEquipment(Guid equipmentId, string name)
    {
        EquipmentId = equipmentId;
        Name = name;
    }
}