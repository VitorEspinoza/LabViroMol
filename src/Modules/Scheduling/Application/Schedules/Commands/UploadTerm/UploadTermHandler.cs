using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LabViroMol.Modules.Scheduling.Application.Shared;
using LabViroMol.Modules.Scheduling.Domain.Schedules;
using LabViroMol.Modules.Shared.Infrastructure.Storage;
using LabViroMol.Modules.Shared.Kernel.Primitives;
using Mediator;
using Microsoft.Extensions.Options;

namespace LabViroMol.Modules.Scheduling.Application.Schedules.Commands.UploadTerm;

public class UploadTermHandler : ICommandHandler<UploadTermCommand, Result>
{
    private readonly IScheduleRepository _scheduleRepository;
    private readonly ISchedulingUnitOfWork _unitOfWork;
    private readonly IFileStorage _storage;
    private readonly StorageSettings _storageSettings;

    public UploadTermHandler(
        IScheduleRepository scheduleRepository,
        ISchedulingUnitOfWork unitOfWork,
        IFileStorage storage,
        IOptions<StorageSettings> storageSettings)
    {
        _scheduleRepository = scheduleRepository;
        _unitOfWork = unitOfWork;
        _storage = storage;
        _storageSettings = storageSettings.Value;
    }

    public async ValueTask<Result> Handle(
        UploadTermCommand command,
        CancellationToken ct)
    {
        var schedule = await _scheduleRepository.GetByIdAsync(
            command.ScheduleId,
            ct);

        if (schedule is null)
            return Result.NotFound(
                "Agendamento não encontrado");

        var extension = Path.GetExtension(
            command.FileName);

        var allowedExtensions = new[]
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png"
        };

        if (!allowedExtensions.Contains(
                extension.ToLowerInvariant()))
        {
            return Result.BusinessRule(
                "O termo deve estar em formato .pdf, .jpg, .jpeg ou .png");
        }

        var fileName =
            $"{schedule.Id.Value}{extension}";

        var termUrl = await _storage.SaveAsync(
            command.Stream,
            fileName,
            _storageSettings.Folders.ScheduleTerms,
            ct);

        schedule.AttachTermUrl(termUrl);

        await _unitOfWork.CompleteAsync(ct);

        return Result.Success();
    }
}