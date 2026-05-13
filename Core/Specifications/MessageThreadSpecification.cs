using Core.Entities;

namespace Core.Specifications
{
    public class MessageThreadSpecification : BaseSpecification<Message>
    {
        // Chat general - aduce DOAR mesajele fara OrderId
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

        // Chat comanda - aduce DOAR mesajele cu OrderId respectiv
        // FIX principal: fiecare user vede doar mesajele unde e implicat ca sender sau recipient
        // => mesajul catre vanzator (sender=buyer, recipient=producer) NU apare la cumparator
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