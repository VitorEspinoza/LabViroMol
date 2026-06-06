using FluentValidation;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.Create;

public class CreateScheduleValidator : AbstractValidator<CreateScheduleCommand>
{
    public CreateScheduleValidator()
    {
        // Scheduler
        RuleFor(x => x.Scheduler.Name)
            .NotEmpty().WithMessage("Nome do agendador é obrigatório")
            .MaximumLength(100).WithMessage("O nome do agendador não pode exceder 100 caracteres");

        RuleFor(x => x.Scheduler.Course)
            .NotEmpty().WithMessage("Curso é obrigatório")
            .MaximumLength(100).WithMessage("O curso não pode exceder 100 caracteres");

        RuleFor(x => x.Scheduler.Email)
            .NotEmpty().WithMessage("Email é obrigatório")
            .EmailAddress().WithMessage("Email inválido");

        // Scheduling
        RuleFor(x => x.Scheduling.Date)
            .NotEmpty().WithMessage("Data do agendamento é obrigatória");

        RuleFor(x => x.Scheduling.Start)
            .NotEmpty().WithMessage("Horário de início é obrigatório");

        RuleFor(x => x.Scheduling.End)
            .NotEmpty().WithMessage("Horário de término é obrigatório");

        // Aceite de termos
        RuleFor(x => x.AcceptTerm)
            .Equal(true)
            .WithMessage("É necessário aceitar os termos para realizar o agendamento");

        // Professor orientador
        RuleFor(x => x.AdvisorProfessor)
            .NotEmpty().WithMessage("Professor orientador é obrigatório")
            .MaximumLength(150).WithMessage("O nome do professor não pode exceder 150 caracteres");

        // Título do projeto
        RuleFor(x => x.ProjectTitle)
            .NotEmpty().WithMessage("Título do projeto é obrigatório")
            .MaximumLength(200).WithMessage("O título do projeto não pode exceder 200 caracteres");

        // Descrição
        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Descrição é obrigatória")
            .MaximumLength(1000).WithMessage("A descrição não pode exceder 1000 caracteres");
    }

}