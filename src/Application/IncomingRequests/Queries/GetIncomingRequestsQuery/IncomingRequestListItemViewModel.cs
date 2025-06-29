namespace ChatbotApi.Application.IncomingRequests.Queries.GetIncomingRequestsQuery
{
    public class IncomingRequestListItemViewModel
    {
        public int Id { get; set; }
        public DateTimeOffset Created { get; set; }
        public string? Endpoint { get; set; }
        public string? Channel { get; set; }
        public string? Raw { get; set; }
    }
}