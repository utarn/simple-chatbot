using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Commands.EditFlexMessageCommand;

public class EditFlexMessageCommand : IRequest<bool>
{
    public int Id { get; set; }
    public int ChatbotId { get; set; }
    [Localize(Value = "ช่องทางบริการ")]
    public string Type { get; set; }

    [Localize(Value = "ข้อความหรือ Postback ที่ต้องการส่ง (ภาษาอังกฤษใช้ตัวอักษรเล็กเท่านั้น)")]
    public string Key { get; set; }

    [Localize(Value = "ลำดับข้อความ")]
    public int Order { get; set; }

    [Localize(Value = "ข้อความตอบกลับ")]
    public string JsonValue { get; set; }

    public class EditFlexMessageCommandHandler : IRequestHandler<EditFlexMessageCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public EditFlexMessageCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> Handle(EditFlexMessageCommand request, CancellationToken cancellationToken)
        {
            var entity = await _context.FlexMessages.FirstAsync(x => x.Id == request.Id, cancellationToken);
            entity.Type = request.Type;
            entity.Key = request.Key;
            entity.Order = request.Order;
            entity.JsonValue = request.JsonValue;

            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
