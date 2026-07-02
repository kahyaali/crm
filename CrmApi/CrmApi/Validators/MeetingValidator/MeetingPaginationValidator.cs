using Crm.Application.DTOs.Meeting;
using FluentValidation;

namespace CrmApi.Validators.MeetingValidator
{
    public class MeetingPaginationValidator : AbstractValidator<MeetingPaginationDto>
    {
        public MeetingPaginationValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1).WithMessage("Sayfa 1'den küçük olamaz");

            RuleFor(x => x.PageSize)
                .GreaterThanOrEqualTo(1).WithMessage("Sayfa boyutu 1'den küçük olamaz")
                .LessThanOrEqualTo(100).WithMessage("Sayfa boyutu 100'den büyük olamaz");
        }
    }
}
