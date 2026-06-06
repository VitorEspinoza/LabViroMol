using Bogus;
using LabViroMol.Modules.Assets.Application.Equipments.Commands.Update;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;

namespace LabViroMol.Modules.Assets.Domain.UnitTests.Common;

public class Fakers
{
    private static readonly Faker Faker = new("pt_BR");

    public static Guid AnyEquipmentId() => Guid.NewGuid();

    #region Equipments
    public static Equipment GenerateEquipment()
    {
        var result = Equipment.Create(
            name: Faker.Commerce.ProductName(),
            brand: Faker.Company.CompanyName(),
            model: Faker.Commerce.Product(),
            code: Faker.Random.AlphaNumeric(8).ToUpper(),
            description: Faker.Lorem.Sentence()
        );

        return result.Data!;
    }

    public static UpdateEquipmentCommand GenerateUpdateCommand(EquipmentId equipmentId)
    {
        return new UpdateEquipmentCommand(
            EquipmentId: equipmentId,
            Name: Faker.Commerce.ProductName(),
            Model: Faker.Commerce.Product(),
            Brand: Faker.Company.CompanyName(),
            Code: Faker.Random.AlphaNumeric(8).ToUpper(),
            Description: Faker.Lorem.Sentence(),
            Location: null
        );
    }

    #endregion

    #region MaintenanceRequest

    public static MaintenanceRequest CreateMaintenanceRequest(
        string? description = null,
        string? problemDescription = null,
        Guid? equipmentId = null)
        => MaintenanceRequest.Create(
            description ?? Faker.Lorem.Sentence(),
            problemDescription ?? Faker.Lorem.Paragraph(),
            equipmentId ?? AnyEquipmentId()).Data!;

    public static MaintenanceRequest CreateInProgressMaintenanceRequest()
    {
        var request = CreateMaintenanceRequest();
        request.Start();
        return request;
    }

    public static MaintenanceRequest CreateDoneMaintenanceRequest()
    {
        var request = CreateInProgressMaintenanceRequest();
        request.Done();
        return request;
    }

    public static MaintenanceRequest CreateCancelledMaintenanceRequest()
    {
        var request = CreateMaintenanceRequest();
        request.Cancel();
        return request;
    }

    #endregion

}
