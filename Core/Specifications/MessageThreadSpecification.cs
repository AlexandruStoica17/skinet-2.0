using Core.Entities;

namespace Core.Specifications
{
    public class MessageThreadSpecification : BaseSpecification<Message>
    {
        public MessageThreadSpecification(string currentUsername, string recipientUsername)
            : base(m =>
                (m.RecipientUsername == currentUsername && m.SenderUsername == recipientUsername && !m.RecipientDeleted) ||
                (m.RecipientUsername == recipientUsername && m.SenderUsername == currentUsername && !m.SenderDeleted)
            )
        {
            AddInclude(x => x.Sender);
            AddInclude(x => x.Recipient);
            AddOrderBy(x => x.MessageSent); // Cele mai vechi primele (cronologic)
        }
    }
}