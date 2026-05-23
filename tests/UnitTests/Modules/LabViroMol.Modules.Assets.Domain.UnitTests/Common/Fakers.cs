using Bogus;
using LabViroMol.Modules.Assets.Application.Equipments.Commands.Update;
using LabViroMol.Modules.Assets.Domain.Equipments;
using LabViroMol.Modules.Assets.Domain.MaintenanceRequests;
using LabViroMol.Modules.Shared.Abstractions.Identity;
using LabViroMol.Modules.Shared.Abstractions.Primitives;

namespace LabViroMol.Modules.Assets.Domain.UnitTests.Common;

public class Fakers
{
    private static readonly Faker Faker = new("pt_BR");
    
    #region Primitives
 
    public static UserId AnyUserId() => IdFactory.New<UserId>();
 
    public static Guid AnyEquipmentId() => Guid.NewGuid();
 
    #endregion

    #region Equipments
    public static (Equipment Equipment, UserId CreatedBy) Generate()
    {
        var createdBy = IdFactory.New<UserId>();

        var result = Equipment.Create(
            createdBy,
            name: Faker.Commerce.ProductName(),
            brand: Faker.Company.CompanyName(),
            model: Faker.Commerce.Product(),
            code: Faker.Random.AlphaNumeric(8).ToUpper(),
            description: Faker.Lorem.Sentence()
        );

        return (result.Data!, createdBy);
    }

    public static UpdateEquipmentCommand GenerateUpdateCommand(EquipmentId equipmentId)
    {
        return new UpdateEquipmentCommand(
            EquipmentId: equipmentId,
            Name: Faker.Commerce.ProductName(),
            Model: Faker.Commerce.Product(),
            Brand: Faker.Company.CompanyName(),
            Code: Faker.Random.AlphaNumeric(8).ToUpper(),
            Description: Faker.Lorem.Sentence()
        );
    }
    
    #endregion
    
    #region MaintenanceRequest
 
    public static MaintenanceRequest CreateMaintenanceRequest(
        UserId? createdBy = null,
        string? description = null,
        string? problemDescription = null,
        Guid? equipmentId = null)
        => MaintenanceRequest.Create(
            createdBy ?? AnyUserId(),
            description ?? Faker.Lorem.Sentence(),
            problemDescription ?? Faker.Lorem.Paragraph(),
            equipmentId ?? AnyEquipmentId()).Data!;
    
    public static MaintenanceRequest CreateInProgressMaintenanceRequest()
    {
        var request = CreateMaintenanceRequest();
        request.Start(AnyUserId());
        return request;
    }
    
    public static MaintenanceRequest CreateDoneMaintenanceRequest()
    {
        var request = CreateInProgressMaintenanceRequest();
        request.Done(AnyUserId());
        return request;
    }
    
    public static MaintenanceRequest CreateCancelledMaintenanceRequest()
    {
        var request = CreateMaintenanceRequest();
        request.Cancel(AnyUserId());
        return request;
    }
 
    #endregion

}