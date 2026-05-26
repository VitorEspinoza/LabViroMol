using LabViroMol.Modules.Research.Domain.Projects;
using LabViroMol.Modules.Research.Domain.UnitTests.Common;

namespace LabViroMol.Modules.Research.Domain.UnitTests.Projects;

public class ProjectTests
{
    public class CreateTests
    {
        [Fact]
        public void Create_ShouldInitializeWithPlannedStatusAndResearchLeadMember()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();

            // Act
            var project = Fakers.CreateProject(leadId);

            // Assert
            Assert.Equal(ProjectStatus.Planned, project.Status);
            Assert.Single(project.Members);
            Assert.Equal(ProjectRole.ResearchLead, project.Members.Single().Role);
        }
    }

    public class StartTests
    {
        [Fact]
        public void Start_WhenPlannedAndByLead_ShouldTransitionToInProgress()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.Start(leadId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ProjectStatus.InProgress, project.Status);
        }

        [Fact]
        public void Start_WhenByCollaborator_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.Start(collaboratorId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Start_WhenAlreadyInProgress_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.Start(leadId);

            // Act
            var result = project.Start(leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Start_WhenCompleted_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.Start(leadId);
            project.Complete(leadId);

            // Act
            var result = project.Start(leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Start_WhenCanceled_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.Cancel(leadId);

            // Act
            var result = project.Start(leadId);

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class CompleteTests
    {
        [Fact]
        public void Complete_WhenInProgressAndByLead_ShouldTransitionToCompleted()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.Start(leadId);

            // Act
            var result = project.Complete(leadId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ProjectStatus.Completed, project.Status);
        }

        [Fact]
        public void Complete_WhenByCollaborator_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);
            project.Start(leadId);

            // Act
            var result = project.Complete(collaboratorId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Complete_WhenPlanned_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.Complete(leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Complete_WhenAlreadyCompleted_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.Start(leadId);
            project.Complete(leadId);

            // Act
            var result = project.Complete(leadId);

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class CancelTests
    {
        [Fact]
        public void Cancel_WhenPlanned_ShouldTransitionToCanceled()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.Cancel(leadId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ProjectStatus.Canceled, project.Status);
        }

        [Fact]
        public void Cancel_WhenInProgress_ShouldTransitionToCanceled()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.Start(leadId);

            // Act
            var result = project.Cancel(leadId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ProjectStatus.Canceled, project.Status);
        }

        [Fact]
        public void Cancel_WhenByCollaborator_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.Cancel(collaboratorId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Cancel_WhenCompleted_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.Start(leadId);
            project.Complete(leadId);

            // Act
            var result = project.Cancel(leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void Cancel_WhenAlreadyCanceled_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.Cancel(leadId);

            // Act
            var result = project.Cancel(leadId);

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class UpdateTests
    {
        [Fact]
        public void Update_WhenByResearchLead_ShouldUpdateProperties()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            var newTitle = "Novo titulo do projeto";
            var newDescription = "Nova descricao do projeto com detalhes";

            // Act
            var result = project.Update(newTitle, newDescription, leadId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(newTitle, project.Title);
            Assert.Equal(newDescription, project.Description);
        }

        [Fact]
        public void Update_WhenByManager_ShouldSucceed()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var managerId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(managerId, ProjectRole.Manager, leadId);

            // Act
            var result = project.Update("Titulo atualizado pelo gerente", "Descricao atualizada", managerId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void Update_WhenByCollaborator_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.Update("Titulo qualquer", "Descricao qualquer", collaboratorId);

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class AddMemberTests
    {
        [Fact]
        public void AddMember_ByLead_NewCollaborator_ShouldAddToMembers()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var newMemberId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.AddMember(newMemberId, ProjectRole.Collaborator, leadId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(2, project.Members.Count(m => !m.IsDeleted));
        }

        [Fact]
        public void AddMember_ByManager_ShouldSucceed()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var managerId = Fakers.AnyResearcherId();
            var newMemberId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(managerId, ProjectRole.Manager, leadId);

            // Act
            var result = project.AddMember(newMemberId, ProjectRole.Collaborator, managerId);

            // Assert
            Assert.True(result.IsSuccess);
        }

        [Fact]
        public void AddMember_ByCollaborator_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.AddMember(Fakers.AnyResearcherId(), ProjectRole.Collaborator, collaboratorId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void AddMember_WhenAlreadyActiveMember_ShouldReturnConflictError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var memberId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(memberId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.AddMember(memberId, ProjectRole.Collaborator, leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void AddMember_WhenAddingSecondResearchLead_ShouldReturnConflictError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var secondLeadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.AddMember(secondLeadId, ProjectRole.ResearchLead, leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void AddMember_WhenReAddingRemovedMember_ShouldRestoreAndUpdateRole()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var memberId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(memberId, ProjectRole.Collaborator, leadId);
            project.RemoveMember(memberId, leadId);

            // Act
            var result = project.AddMember(memberId, ProjectRole.Manager, leadId);

            // Assert
            Assert.True(result.IsSuccess);
            var restored = project.Members.Single(m => m.Id == memberId);
            Assert.False(restored.IsDeleted);
            Assert.Equal(ProjectRole.Manager, restored.Role);
        }

        [Fact]
        public void AddMember_WhenReAddingRemovedMemberAsLeadWhileLeadExists_ShouldReturnConflictError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var memberId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(memberId, ProjectRole.Collaborator, leadId);
            project.RemoveMember(memberId, leadId);

            // Act
            var result = project.AddMember(memberId, ProjectRole.ResearchLead, leadId);

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class RemoveMemberTests
    {
        [Fact]
        public void RemoveMember_ByLead_TargetIsCollaborator_ShouldSoftDeleteMember()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.RemoveMember(collaboratorId, leadId);

            // Assert
            Assert.True(result.IsSuccess);
            var member = project.Members.Single(m => m.Id == collaboratorId);
            Assert.True(member.IsDeleted);
        }

        [Fact]
        public void RemoveMember_WhenTargetIsResearchLead_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.RemoveMember(leadId, leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void RemoveMember_WhenMemberNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            var unknownId = Fakers.AnyResearcherId();

            // Act
            var result = project.RemoveMember(unknownId, leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void RemoveMember_ByCollaborator_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var targetId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);
            project.AddMember(targetId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.RemoveMember(targetId, collaboratorId);

            // Assert
            Assert.True(result.IsFailure);
        }
    }

    public class TransferLeadershipTests
    {
        [Fact]
        public void TransferLeadership_ByLead_ShouldPromoteTargetAndDemoteCurrentLead()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.TransferLeadership(collaboratorId, leadId);

            // Assert
            Assert.True(result.IsSuccess);
            var newLead = project.Members.Single(m => m.Id == collaboratorId);
            var oldLead = project.Members.Single(m => m.Id == leadId);
            Assert.Equal(ProjectRole.ResearchLead, newLead.Role);
            Assert.Equal(ProjectRole.Manager, oldLead.Role);
        }

        [Fact]
        public void TransferLeadership_ByNonLead_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var managerId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(managerId, ProjectRole.Manager, leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.TransferLeadership(collaboratorId, managerId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void TransferLeadership_ToNonExistentMember_ShouldReturnNotFoundError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.TransferLeadership(Fakers.AnyResearcherId(), leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void TransferLeadership_ToCurrentLead_ShouldSucceedWithNoChange()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.TransferLeadership(leadId, leadId);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(ProjectRole.ResearchLead, project.Members.Single(m => m.Id == leadId).Role);
        }
    }

    public class ChangeMemberRoleTests
    {
        [Fact]
        public void ChangeMemberRole_ByLead_ShouldChangeCollaboratorToManager()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.ChangeMemberRole(collaboratorId, ProjectRole.Manager, leadId);

            // Assert
            Assert.True(result.IsSuccess);
            var member = project.Members.Single(m => m.Id == collaboratorId);
            Assert.Equal(ProjectRole.Manager, member.Role);
        }

        [Fact]
        public void ChangeMemberRole_ToResearchLead_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.ChangeMemberRole(collaboratorId, ProjectRole.ResearchLead, leadId);

            // Assert
            Assert.True(result.IsFailure);
            Assert.Contains("TransferLeadership", result.Errors[0]);
        }

        [Fact]
        public void ChangeMemberRole_DemotingCurrentLead_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.ChangeMemberRole(leadId, ProjectRole.Manager, leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void ChangeMemberRole_WhenSameRole_ShouldSucceedWithNoChange()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.ChangeMemberRole(collaboratorId, ProjectRole.Collaborator, leadId);

            // Assert
            Assert.True(result.IsSuccess);
            var member = project.Members.Single(m => m.Id == collaboratorId);
            Assert.Equal(ProjectRole.Collaborator, member.Role);
        }

        [Fact]
        public void ChangeMemberRole_WhenMemberNotFound_ShouldReturnNotFoundError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);

            // Act
            var result = project.ChangeMemberRole(Fakers.AnyResearcherId(), ProjectRole.Manager, leadId);

            // Assert
            Assert.True(result.IsFailure);
        }

        [Fact]
        public void ChangeMemberRole_ByCollaborator_ShouldReturnBusinessRuleError()
        {
            // Arrange
            var leadId = Fakers.AnyResearcherId();
            var collaboratorId = Fakers.AnyResearcherId();
            var targetId = Fakers.AnyResearcherId();
            var project = Fakers.CreateProject(leadId);
            project.AddMember(collaboratorId, ProjectRole.Collaborator, leadId);
            project.AddMember(targetId, ProjectRole.Collaborator, leadId);

            // Act
            var result = project.ChangeMemberRole(targetId, ProjectRole.Manager, collaboratorId);

            // Assert
            Assert.True(result.IsFailure);
        }
    }
}
