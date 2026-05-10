using System.Runtime.Serialization;

namespace Core.Entities.OrderAggregate
{
    public enum OrderStatus
    {
        [EnumMember(Value = "Pending")]
        Pending,

        [EnumMember(Value = "Payment Received")]
        PaymentRecevied,

        [EnumMember(Value = "Payment Failed")]
        PaymentFailed,

        [EnumMember(Value = "Shipped")]
        Shipped,

        // NOU: cumparatorul confirma ca a primit comanda
        [EnumMember(Value = "Delivered")]
        Delivered
    }
}