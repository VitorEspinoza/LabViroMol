# Entity-Relationship Diagram — Research Module

**English** · [Português](./er-diagram.pt-BR.md)

This document presents the ER blocks of the `research` schema. DbContext:
`ResearchDbContext`. The schema was split into **3 cohesive sub-blocks** (Project,
Researcher, Publication) since it is the schema with the most tables (7) and the only one
with 4 intra-schema FK relationships crossing all tables with each other. The
`Researchers` table is referenced from two of the three sub-blocks (Project and
Publication) — in those cases it appears with the FK column annotated as usual, but
without the table's full definition, with a note pointing to the "Researcher" sub-block.

## Index

1. [Project](#project)
2. [Researcher](#researcher)
3. [Publication](#publication)

---

## Project

Sub-block of the `research` schema covering the **Project** grouping: `Partners`,
`Projects` and `ProjectMembers`. `ProjectMembers.ResearcherId` references `Researchers`,
detailed in the [Researcher](#researcher) sub-block — here it appears only as a column
reference, without the table's full definition.

```mermaid
erDiagram
 Partners {
 uuid Id PK
 varchar Name
 varchar Description
 timestamptz CreatedAt
 uuid CreatedBy
 boolean IsDeleted
 timestamptz RemovedAt
 uuid RemovedBy
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 Projects {
 uuid Id PK
 varchar Title
 varchar Description
 varchar Status "ProjectStatus enum as string"
 uuid PartnerId FK
 text Translations "serialized JSON (Dictionary of ProjectTranslation)"
 timestamptz CreatedAt
 uuid CreatedBy
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 ProjectMembers {
 uuid Id PK
 uuid ResearcherId FK "see the 'Researcher' block"
 varchar Role "ProjectRole enum as string"
 timestamptz JoinedAt
 timestamptz LeftAt
 timestamptz CreatedAt
 uuid CreatedBy
 uuid ProjectId FK
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 Partners ||--o{ Projects: "1:N (FK_Projects_Partners_PartnerId, restrict)"
 Projects ||--o{ ProjectMembers: "1:N (FK_ProjectMembers_Projects_ProjectId, cascade)"
```

> Note: `ProjectMembers.ResearcherId` has a real database FK constraint
> (`FK_ProjectMembers_Researchers_ResearcherId`, `ON DELETE RESTRICT`) pointing to
> `Researchers`, a table defined in the [Researcher](#researcher) sub-block — omitted here
> for readability, since it belongs to another cohesive grouping of the same schema.

---

## Researcher

Sub-block of the `research` schema covering the **Researcher** grouping: `Positions` and
`Researchers`.

```mermaid
erDiagram
 Positions {
 uuid Id PK
 varchar Name
 varchar Description
 text Translations "serialized JSON (Dictionary of PositionTranslation)"
 timestamptz CreatedAt
 uuid CreatedBy
 boolean IsDeleted
 timestamptz RemovedAt
 uuid RemovedBy
 }

 Researchers {
 uuid Id PK
 varchar FirstName
 varchar LastName
 varchar CitationName
 text Name_DisplayName
 varchar LattesUrl
 varchar DegreeLevel "DegreeLevel enum as string"
 varchar FieldOfStudy
 uuid PositionId FK
 timestamptz DeactivatedAt
 timestamptz CreatedAt
 uuid CreatedBy
 boolean IsDeleted
 timestamptz RemovedAt
 uuid RemovedBy
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 Positions ||--o{ Researchers: "1:N (FK_Researchers_Positions_PositionId, restrict)"
```

> Note: `Researchers` is referenced, by a real database FK, from
> `ProjectMembers.ResearcherId` (see [Project](#project)) and from
> `PublicationResearchers.ResearcherId` (see [Publication](#publication)) — both
> `ON DELETE RESTRICT`, via migration (not yet applied in any environment).

---

## Publication

Sub-block of the `research` schema covering the **Publication** grouping: `Publications`
and `PublicationResearchers`. `PublicationResearchers.ResearcherId` references
`Researchers`, detailed in the [Researcher](#researcher) sub-block.

```mermaid
erDiagram
 Publications {
 uuid Id PK
 varchar Title
 varchar Description
 varchar Doi
 date PublicationDate
 varchar PublishedOn
 varchar PublishUrl
 text Translations "serialized JSON (Dictionary of PublicationTranslation)"
 timestamptz CreatedAt
 uuid CreatedBy
 boolean IsDeleted
 timestamptz RemovedAt
 uuid RemovedBy
 timestamptz UpdatedAt
 uuid UpdatedBy
 }

 PublicationResearchers {
 uuid PublicationId PK,FK
 uuid ResearcherId PK,FK "see the 'Researcher' block"
 int Order
 }

 Publications ||--o{ PublicationResearchers: "1:N (FK_PublicationResearchers_Publications_PublicationId, cascade)"
```

> Note: `PublicationResearchers.ResearcherId` has a real database FK constraint
> (`FK_PublicationResearchers_Researchers_ResearcherId`, `ON DELETE RESTRICT`) pointing to
> `Researchers`, a table defined in the [Researcher](#researcher) sub-block — omitted here
> for readability. All four intra-schema FKs of the Research module
> (`Researchers.PositionId`, `Projects.PartnerId`, `ProjectMembers.ResearcherId`,
> `PublicationResearchers.ResearcherId`) have not yet been applied in any environment.
