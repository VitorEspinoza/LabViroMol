using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.UnitTests.Common;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;
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
        var (equipment, _) = Fakers.Generate();

        Assert.NotNull(equipment);
    }

    [Fact]
    public void Create_ShouldSetFieldsCorrectly()
    {
        var createdBy = IdFactory.New<UserId>();
        var result = Equipment.Create(createdBy, "Nome", "Marca", "Modelo", "COD001", "Descrição");

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
        var (equipment1, _) = Fakers.Generate();
        var (equipment2, _) = Fakers.Generate();

        Assert.NotEqual(equipment1.Id, equipment2.Id);
    }

    // -------------------------
    // Update
    // -------------------------

    [Fact]
    public void Update_WithValidData_ShouldUpdateFields()
    {
        var (equipment, _) = Fakers.Generate();
        var modifiedBy = IdFactory.New<UserId>();

        equipment.Update("Novo Nome", "Nova Marca", "Novo Modelo", "COD999", "Nova Descrição", modifiedBy);

        Assert.Equal("Novo Nome", equipment.Name);
        Assert.Equal("Nova Marca", equipment.Brand);
        Assert.Equal("Novo Modelo", equipment.Model);
        Assert.Equal("COD999", equipment.Code);
        Assert.Equal("Nova Descrição", equipment.Description);
    }

    [Fact]
    public void Update_ShouldMarkEntityAsUpdated()
    {
        var (equipment, _) = Fakers.Generate();
        var modifiedBy = IdFactory.New<UserId>();

        equipment.Update("Nome", "Marca", "Modelo", "COD001", "Desc", modifiedBy);

        Assert.NotNull(equipment.UpdatedAt);
        Assert.Equal(modifiedBy, equipment.UpdatedBy);
    }

    [Fact]
    public void Update_WithFakeData_ShouldSucceed()
    {
        var (equipment, _) = Fakers.Generate();
        var command = Fakers.GenerateUpdateCommand(equipment.Id);
        var updatedBy = IdFactory.New<UserId>();
        
        equipment.Update(command.Name, command.Brand, command.Model, command.Code, command.Description, updatedBy);

        Assert.Equal(command.Name, equipment.Name);
        Assert.Equal(command.Brand, equipment.Brand);
        Assert.Equal(command.Model, equipment.Model);
        Assert.Equal(command.Code, equipment.Code);
        Assert.Equal(command.Description, equipment.Description);
    }
}