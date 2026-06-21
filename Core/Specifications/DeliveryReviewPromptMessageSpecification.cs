using Core.Entities;

namespace Core.Specifications
{
    public class DeliveryReviewPromptMessageSpecification : BaseSpecification<Message>
    {
        public DeliveryReviewPromptMessageSpecification(int orderId, string producerEmail, string buyerEmail)
            : base(m =>
                m.OrderId == orderId &&
                m.IsSystemMessage &&
                m.IsReviewPrompt &&
                m.SenderUsername == producerEmail &&
                m.RecipientUsername == buyerEmail)
        {
        }
    }
}
