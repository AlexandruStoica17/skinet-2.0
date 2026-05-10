using Core.Entities;

namespace Core.Specifications
{
    /// <summary>
    /// Specificație pentru numărarea mesajelor primite dar NECITITE de un utilizator.
    /// Un mesaj e "necitit" dacă: este destinat acestui user, nu e șters de el, și DateRead este null.
    /// </summary>
    public class UnreadMessagesSpecification : BaseSpecification<Message>
    {
        public UnreadMessagesSpecification(string recipientEmail)
            : base(m =>
                m.RecipientUsername == recipientEmail &&
                !m.RecipientDeleted &&
                m.DateRead == null
            )
        {
        }
    }
}