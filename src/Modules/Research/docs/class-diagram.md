# Class Diagram — Research Module

**English** · [Português](./class-diagram.pt-BR.md)

This document presents the domain class diagrams of the **Research** module.
The module was modeled as **3 cohesive sub-diagrams** (Project, Researcher, Publication)
instead of a single block, since it is the densest grouping in the domain (14 classes and
4 intra-module FKs). `Researcher` is referenced by two of the three sub-diagrams (Project
and Publication) — in those cases it appears as a **stub** class (`<<stub>>`, Id only) with
a note pointing to the "Researcher" sub-diagram, which contains the full definition.

## Index

1. [Project](#project)
2. [Researcher](#researcher)
3. [Publication](#publication)

---

## Project

Covers the **Project** grouping: the aggregate root `Project`, its child entity
`ProjectMember` (composition), and the institutional partner `Partner`. `Researcher` is
referenced by `ProjectMember.ResearcherId`, but its full definition lives in the
[Researcher](#researcher) sub-diagram — here it appears only as a stub class to preserve
readability.

```mermaid
classDiagram
 direction LR

 class Partner {
 +Name: string
 +Description: string
 +Create(name, description) Result~Partner~
 +Update(name, description) Result
 }
 Partner --|> AggregateRoot~PartnerId~
 Partner..|> IFullAuditable

 class Project {
 +Title: string
 +Description: string
 +Status: ProjectStatus
 +PartnerId: PartnerId
 +Translations: Dictionary~string, ProjectTranslation~
 +Create(principalInvestigatorId, title, description, partnerId) Result~Project~
 +Start(requestedBy) Result
 +Complete(requestedBy) Result
 +Cancel(requestedBy) Result
 +Update(title, description, requestedBy) Result
 +AddMember(researcherId, role, requestedBy) Result
 +TransferLeadership(newLeadId, requestedBy) Result
 +ChangeMemberRole(researcherId, newRole, requestedBy) Result
 +RemoveMember(researcherId, requestedBy) Result
 }
 Project --|> AggregateRoot~ProjectId~
 Project..|> ICreationAuditable
 Project..|> IModificationAuditable
 Project..|> ITranslatable~ProjectTranslation~
 Project "many" --> "1" Partner: PartnerId

 class ProjectTranslation {
 <<value object>>
 +Title: string
 +Description: string
 }
 Project "1" *-- "many" ProjectTranslation: Translations

 class ProjectMember {
 +ResearcherId: ResearcherId
 +Role: ProjectRole
 +JoinedAt: DateTimeOffset
 +LeftAt: DateTimeOffset
 +IsActive: bool
 }
 ProjectMember --|> BaseEntity~ProjectMemberId~
 ProjectMember..|> ICreationAuditable
 ProjectMember..|> IModificationAuditable
 Project "1" *-- "many" ProjectMember: members
 ProjectMember..> Researcher: ResearcherId

 class ProjectRole {
 <<enumeration>>
 +ResearchLead
 +Manager
 +Collaborator
 }
 ProjectMember --> ProjectRole: Role

 class ProjectStatus {
 <<enumeration>>
 +Planned
 +InProgress
 +Completed
 +Canceled
 }
 Project --> ProjectStatus: Status

 class Researcher {
 <<stub>>
 +ResearcherId Id
 }
 note for Researcher "See the 'Research — Researcher' diagram\nfor the full aggregate root definition."
```

---

## Researcher

Covers the **Researcher** grouping: the aggregate root `Researcher` and the institutional
position `Position` it links to, together with the value objects and SmartEnums specific
to this grouping.

```mermaid
classDiagram
 direction LR

 class Position {
 +Name: string
 +Description: string
 +Translations: Dictionary~string, PositionTranslation~
 +Create(name, description) Result~Position~
 +AddTranslation(languageCode, name, description) void
 +GetName(language) string
 +GetDescription(language) string
 }
 Position --|> AggregateRoot~PositionId~
 Position..|> ICreationAuditable
 Position..|> IDeletionAuditable
 Position..|> ITranslatable~PositionTranslation~

 class PositionTranslation {
 <<value object>>
 +Name: string
 +Description: string
 }
 Position "1" *-- "many" PositionTranslation: Translations

 class Researcher {
 +Name: ResearcherName
 +LattesUrl: string
 +AcademicBackground: AcademicBackground
 +PositionId: PositionId
 +DeactivatedAt: DateTimeOffset
 +IsActive: bool
 +Create(researcherId, name, lattesUrl, academicBackground, positionId) Researcher
 +Update(degreeLevel, fieldOfStudy, positionId) void
 +UpdateName(name) void
 +Deactivate() void
 +Reactivate() void
 }
 Researcher --|> AggregateRoot~ResearcherId~
 Researcher..|> IFullAuditable
 Researcher "many" --> "1" Position: PositionId

 class ResearcherName {
 <<value object>>
 +FirstName: string
 +LastName: string
 +CitationName: string
 +DisplayName: string
 +FullName: string
 +PublicDisplayName: string
 +PublicCitationName: string
 }
 Researcher "1" --> "1" ResearcherName: Name

 class AcademicBackground {
 <<value object>>
 +DegreeLevel: DegreeLevel
 +FieldOfStudy: string
 }
 Researcher "1" --> "1" AcademicBackground: AcademicBackground

 class DegreeLevel {
 <<enumeration>>
 +Undergraduate
 +Specialization
 +Masters
 +Doctorate
 +PostDoctorate
 }
 AcademicBackground --> DegreeLevel: DegreeLevel
```

> `Researcher` is referenced, by Id, from `ProjectMember` (see
> [Project](#project)) and from `PublicationResearcher` (see
> [Publication](#publication)) — in both other
> sub-diagrams it appears only as a stub class.

---

## Publication

Covers the **Publication** grouping: the aggregate root `Publication` and its child entity
`PublicationResearcher` (composition, ordered list of authors). `Researcher` is referenced
by `PublicationResearcher.ResearcherId`, but its full definition lives in the
[Researcher](#researcher) sub-diagram.

```mermaid
classDiagram
 direction LR

 class Publication {
 +Title: string
 +Description: string
 +Doi: string
 +PublicationDate: DateOnly
 +PublishedOn: string
 +PublishUrl: string
 +Translations: Dictionary~string, PublicationTranslation~
 +Create(title, description, doi, publicationDate, publishedOn, publishUrl) Result~Publication~
 +Update(title, description, publishedOn, publishUrl) Result
 +AssignDoi(doi) Result
 +AddResearcher(researcherId) Result
 +RemoveResearcher(researcherId) Result
 +ReorderResearchers(orderedIds) Result
 }
 Publication --|> AggregateRoot~PublicationId~
 Publication..|> IFullAuditable
 Publication..|> ITranslatable~PublicationTranslation~

 class PublicationTranslation {
 <<value object>>
 +Title: string
 +Description: string
 }
 Publication "1" *-- "many" PublicationTranslation: Translations

 class PublicationResearcher {
 <<value object>>
 +ResearcherId: ResearcherId
 +Order: int
 }
 Publication "1" *-- "many" PublicationResearcher: researchers
 PublicationResearcher..> Researcher: ResearcherId

 class Researcher {
 <<stub>>
 +ResearcherId Id
 }
 note for Researcher "See the 'Research — Researcher' diagram\nfor the full aggregate root definition."
```
