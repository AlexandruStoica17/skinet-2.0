using Core.Entities;

namespace Core.Specifications
{
    public class MessageThreadSpecification : BaseSpecification<Message>
    {
        // Constructor original — pentru chat general intre doi useri (fara orderId)
        public MessageThreadSpecification(string currentUsername, string recipientUsername)
            : base(m =>
                m.OrderId == null &&
                (
                    (m.RecipientUsername == currentUsername && m.SenderUsername == recipientUsername && !m.RecipientDeleted) ||
                    (m.RecipientUsername == recipientUsername && m.SenderUsername == currentUsername && !m.SenderDeleted)
                )
            )
        {
            AddInclude(x => x.Sender);
            AddInclude(x => x.Recipient);
            AddOrderBy(x => x.MessageSent);
        }

        // NOU: Constructor pentru chat legat de o comanda specifica
        public MessageThreadSpecification(string currentUsername, string recipientUsername, int orderId)
            : base(m =>
                m.OrderId == orderId &&
                (
                    (m.RecipientUsername == currentUsername && m.SenderUsername == recipientUsername && !m.RecipientDeleted) ||
                    (m.RecipientUsername == recipientUsername && m.SenderUsername == currentUsername && !m.SenderDeleted)
                )
            )
        {
            AddInclude(x => x.Sender);
            AddInclude(x => x.Recipient);
            AddOrderBy(x => x.MessageSent);
        }
    }
}