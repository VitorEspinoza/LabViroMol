# Diagrama de Classes — Módulo Research

[English](./class-diagram.md) · **Português**

Este documento apresenta os diagramas de classes do domínio do módulo **Research**.
O módulo foi modelado em **3 sub-diagramas coesos** (Projeto, Pesquisador, Publicação) em
vez de um único bloco, por ser o agrupamento mais denso do domínio (14 classes e 4 FKs
intra-módulo). `Researcher` é referenciado por dois dos três sub-diagramas (Projeto e
Publicação) — nesses casos aparece como classe **stub** (`<<stub>>`, apenas Id) com nota
apontando para o sub-diagrama "Pesquisador", que contém a definição completa.

## Índice

1. [Projeto](#projeto)
2. [Pesquisador](#pesquisador)
3. [Publicação](#publicação)

---

## Projeto

Cobre o agrupamento de **Projeto**: o aggregate root `Project`, sua entidade filha
`ProjectMember` (composição) e o parceiro institucional `Partner`. `Researcher` é
referenciado por `ProjectMember.ResearcherId`, mas sua definição completa está no
sub-diagrama [Pesquisador](#pesquisador) — aqui ele aparece apenas como classe stub para
preservar a legibilidade.

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
 note for Researcher "Ver diagrama 'Research — Pesquisador'\npara a definição completa do aggregate root."
```

---

## Pesquisador

Cobre o agrupamento de **Pesquisador**: o aggregate root `Researcher` e o cargo/posição
institucional `Position` ao qual ele se vincula, com os value objects e SmartEnums
específicos desse agrupamento.

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

> `Researcher` é referenciado, por Id, a partir de `ProjectMember` (ver
> [Projeto](#projeto)) e de `PublicationResearcher` (ver
> [Publicação](#publicação)) — em ambos os outros
> sub-diagramas ele aparece apenas como classe stub.

---

## Publicação

Cobre o agrupamento de **Publicação**: o aggregate root `Publication` e sua entidade
filha `PublicationResearcher` (composição, lista ordenada de autores). `Researcher` é
referenciado por `PublicationResearcher.ResearcherId`, mas sua definição completa está
no sub-diagrama [Pesquisador](#pesquisador).

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
 note for Researcher "Ver diagrama 'Research — Pesquisador'\npara a definição completa do aggregate root."
```
