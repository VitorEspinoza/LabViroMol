using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.UnitTests.Common;
using Xunit;

namespace LabViroMol.Modules.Assets.Domain.UnitTests.Equipments;

public class EquipmentTests
{
    // -------------------------
    // Create
    // -------------------------

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        var equipment = Fakers.GenerateEquipment();

        Assert.NotNull(equipment);
    }

    [Fact]
    public void Create_ShouldSetFieldsCorrectly()
    {
        var result = Equipment.Create("Nome", "Marca", "Modelo", "COD001", "Descrição");

        Assert.True(result.IsSuccess);
        Assert.Equal("Nome", result.Data!.Name);
        Assert.Equal("Marca", result.Data.Brand);
        Assert.Equal("Modelo", result.Data.Model);
        Assert.Equal("COD001", result.Data.Code);
        Assert.Equal("Descrição", result.Data.Description);
    }

    [Fact]
    public void Create_MultipleEquipments_ShouldHaveUniqueIds()
    {
        var equipment1 = Fakers.GenerateEquipment();
        var equipment2 = Fakers.GenerateEquipment();

        Assert.NotEqual(equipment1.Id, equipment2.Id);
    }

    // -------------------------
    // Update
    // -------------------------

    [Fact]
    public void Update_WithValidData_ShouldUpdateFields()
    {
        var equipment = Fakers.GenerateEquipment();

        equipment.Update("Novo Nome", "Nova Marca", "Novo Modelo", "COD999", "Nova Descrição");

        Assert.Equal("Novo Nome", equipment.Name);
        Assert.Equal("Nova Marca", equipment.Brand);
        Assert.Equal("Novo Modelo", equipment.Model);
        Assert.Equal("COD999", equipment.Code);
        Assert.Equal("Nova Descrição", equipment.Description);
    }

    [Fact]
    public void Update_WithFakeData_ShouldSucceed()
    {
        var equipment = Fakers.GenerateEquipment();
        var command = Fakers.GenerateUpdateCommand(equipment.Id);

        equipment.Update(command.Name, command.Brand, command.Model, command.Code, command.Description);

        Assert.Equal(command.Name, equipment.Name);
        Assert.Equal(command.Brand, equipment.Brand);
        Assert.Equal(command.Model, equipment.Model);
        Assert.Equal(command.Code, equipment.Code);
        Assert.Equal(command.Description, equipment.Description);
    }
}
