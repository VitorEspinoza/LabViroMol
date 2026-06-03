using System;
using Bogus;
using LabViroMol.Modules.Research.Domain.Partners;
using LabViroMol.Modules.Research.Domain.Positions;
using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.Publications;
using LabViroMol.Modules.Research.Domain.Researchers;
using LabViroMol.Modules.Shared.Kernel.Identity;
using LabViroMol.Modules.Shared.Kernel.Primitives;

namespace LabViroMol.Modules.Research.Domain.UnitTests.Common;

internal static class Fakers
{
    private static readonly Faker F = new("pt_BR");

    #region Primitives

    public static UserId AnyUserId() => IdFactory.New<UserId>();
    public static ResearcherId AnyResearcherId() => IdFactory.New<ResearcherId>();
    public static PositionId AnyPositionId() => IdFactory.New<PositionId>();
    public static PartnerId AnyPartnerId() => IdFactory.New<PartnerId>();
    public static ProjectId AnyProjectId() => IdFactory.New<ProjectId>();
    public static PublicationId AnyPublicationId() => IdFactory.New<PublicationId>();

    #endregion

    #region Value Objects

    public static ResearcherName AnyResearcherName()
        => new(F.Name.FirstName(), F.Name.LastName(), null, null);

    public static AcademicBackground AnyAcademicBackground()
        => new(DegreeLevel.Masters, F.Lorem.Word());

    #endregion

    #region Position

    public static Position CreatePosition(string? name = null)
        => Position.Create(name ?? F.Name.JobTitle(), F.Lorem.Sentence()).Data!;

    #endregion

    #region Partner

    public static Partner CreatePartner(string? name = null)
        => Partner.Create(name ?? F.Company.CompanyName(), F.Lorem.Sentence()).Data!;

    #endregion

    #region Researcher

    public static Researcher CreateResearcher(ResearcherId? id = null)
        => Researcher.Create(
            id ?? AnyResearcherId(),
            AnyResearcherName(),
            null,
            AnyAcademicBackground(),
            AnyPositionId());

    #endregion

    #region Project

    public static Project CreateProject(ResearcherId? leadId = null)
        => Project.Create(
            leadId ?? AnyResearcherId(),
            F.Lorem.Sentence(),
            F.Lorem.Paragraph(),
            AnyPartnerId()).Data!;

    #endregion

    #region Publication

    public static Publication CreatePublication()
        => Publication.Create(
            F.Lorem.Sentence(5),
            F.Lorem.Paragraph(),
            "10.1234/test",
            new DateOnly(2024, 1, 1),
            "Nature",
            "https://example.com").Data!;

    #endregion
}
