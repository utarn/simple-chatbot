using Utharn.Library.Localizer;

namespace ChatbotApi.Application.Chatbots.Commands.CreateFlexMessageCommand;

public class CreateFlexMessageCommand : IRequest<bool>
{
    [Localize(Value = "แชทบอท")]
    public int ChatbotId { get; set; }
    [Localize(Value = "ช่องทางบริการ")]
    public string Type { get; set; }
    [Localize(Value = "ข้อความหรือ Postback ที่ต้องการส่ง (ภาษาอังกฤษใช้ตัวอักษรเล็กเท่านั้น)")]
    public string Key { get; set; }
    [Localize(Value ="ลำดับข้อความ (1-100)")]
    public int Order { get; set; }
    [Localize(Value = "ข้อความตอบกลับ")]
    public string JsonValue { get; set; }

    public class CreateFlexMessageCommandHandler : IRequestHandler<CreateFlexMessageCommand, bool>
    {
        private readonly IApplicationDbContext _context;

        public CreateFlexMessageCommandHandler(IApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<bool> Handle(CreateFlexMessageCommand request, CancellationToken cancellationToken)
        {
            var entity = new FlexMessage
            {
                ChatbotId = request.ChatbotId,
                Type = request.Type,
                Key = request.Key,
                Order = request.Order,
                JsonValue = request.JsonValue
            };

            await _context.FlexMessages.AddAsync(entity, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
