using Core.Entities;

namespace Core.Specifications
{
    public class AllUserMessagesSpecification : BaseSpecification<Message>
    {
        public AllUserMessagesSpecification(string currentEmail)
            : base(m =>
                (m.SenderUsername == currentEmail && !m.SenderDeleted) ||
                (m.RecipientUsername == currentEmail && !m.RecipientDeleted)
            )
        {
            AddInclude(m => m.Sender);
            AddInclude(m => m.Recipient);
            AddOrderByDescending(m => m.MessageSent);
        }
    }
}