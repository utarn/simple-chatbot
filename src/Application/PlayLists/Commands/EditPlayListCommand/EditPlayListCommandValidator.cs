namespace ChatbotApi.Application.PlayLists.Commands.EditPlayListCommand;

public class EditPlayListCommandValidator : AbstractValidator<EditPlayListCommand>
{
    public EditPlayListCommandValidator()
    {
        RuleFor(v => v.Id)
            .NotEmpty()
            .WithMessage("Id ต้องไม่ว่าง")
            .GreaterThan(0)
            .WithMessage("Id ต้องมากกว่า 0");

        RuleFor(v => v.MusicName)
            .NotEmpty()
            .WithMessage("ชื่อเพลงต้องไม่ว่าง")
            .MaximumLength(200)
            .WithMessage("ชื่อเพลงต้องไม่เกิน 200 ตัวอักษร");

        RuleFor(v => v.AlbumName)
            .NotEmpty()
            .WithMessage("ชื่ออัลบั้มต้องไม่ว่าง")
            .MaximumLength(200)
            .WithMessage("ชื่ออัลบั้มต้องไม่เกิน 200 ตัวอักษร");
    }
}