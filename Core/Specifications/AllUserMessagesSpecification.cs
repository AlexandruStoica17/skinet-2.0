using Core.Entities;

namespace Core.Specifications
{
    // Aduce toate mesajele în care userul curent e sender SAU recipient
    // Folosit pentru a construi lista de conversații (inbox)
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