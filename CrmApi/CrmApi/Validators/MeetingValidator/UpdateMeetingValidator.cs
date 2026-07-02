using Crm.Application.DTOs.Meeting;
using FluentValidation;

namespace CrmApi.Validators.MeetingValidator
{
    public class UpdateMeetingValidator : AbstractValidator<UpdateMeetingDto>
    {
        public UpdateMeetingValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0).WithMessage("Geçersiz toplantı ID");

            RuleFor(x => x.Title)
                .NotEmpty().WithMessage("Toplantı başlığı zorunludur")
                .MaximumLength(200).WithMessage("Başlık en fazla 200 karakter olabilir");

            RuleFor(x => x.StartTime)
                .NotEmpty().WithMessage("Başlangıç zamanı zorunludur");

            RuleFor(x => x.EndTime)
                .NotEmpty().WithMessage("Bitiş zamanı zorunludur")
                .GreaterThan(x => x.StartTime)
                .WithMessage("Bitiş zamanı, başlangıç zamanından sonra olmalıdır");

            RuleFor(x => x.Status)
                .Must(s => string.IsNullOrEmpty(s) || new[] { "Planlandı", "Devam Ediyor", "Tamamlandı", "İptal" }.Contains(s))
                .WithMessage("Geçersiz durum değeri");
        }
    }
}
