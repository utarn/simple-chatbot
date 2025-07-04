namespace ChatbotApi.Application.PlayLists.Commands.CreatePlayListCommand;

public class CreatePlayListCommandValidator : AbstractValidator<CreatePlayListCommand>
{
    public CreatePlayListCommandValidator()
    {
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