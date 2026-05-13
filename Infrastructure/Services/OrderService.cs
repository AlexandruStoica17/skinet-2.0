using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Entities.Identity;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Identity;

using ShippingAddress = Core.Entities.OrderAggregate.Address;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IBasketRepository _basketRepo;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public OrderService(IBasketRepository basketRepo, IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _unitOfWork = unitOfWork;
            _basketRepo = basketRepo;
            _userManager = userManager;
        }

        public async Task<Order> CreateOrderAsync(string buyerEmail, int deliveryMethodId, string basketId, ShippingAddress shippingAddress)
        {
            var basket = await _basketRepo.GetBasketAsync(basketId);

            var items = new List<OrderItem>();
            foreach (var item in basket.Items)
            {
                var productItem = await _unitOfWork.Repository<Product>().GetByIdAsync(item.Id);
                var itemOrdered = new ProductItemOrdered(productItem.Id, productItem.Name, productItem.PictureUrl);
                
                var producerId = string.IsNullOrEmpty(productItem.ProducerId) ? "Admin" : productItem.ProducerId;
                var producerName = string.IsNullOrEmpty(productItem.ProducerName) ? "Magazinul Nostru" : productItem.ProducerName;

                var orderItem = new OrderItem(itemOrdered, productItem.Price, item.Quantity, producerId, producerName);
                items.Add(orderItem);
            }

            var deliveryMethod = await _unitOfWork.Repository<DeliveryMethod>().GetByIdAsync(deliveryMethodId);
            var subtotal = items.Sum(item => item.Price * item.Quantity);

            var spec = new OrderByPaymentIntentIdSpecification(basket.PaymentIntentId);
            var order = await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec);

            if (order != null)
            {
                order.ShipToAddress = shippingAddress;
                order.DeliveryMethod = deliveryMethod;
                order.Subtotal = subtotal;
                _unitOfWork.Repository<Order>().Update(order);
            }
            else
            {
                order = new Order(items, buyerEmail, shippingAddress, deliveryMethod, subtotal, basket.PaymentIntentId);
                _unitOfWork.Repository<Order>().Add(order);
            }

            var result = await _unitOfWork.Complete();
            if (result <= 0) return null;

            // Trimitem mesaje automate cu OrderId — fiecare comanda = conversatie separata
            var buyer = await _userManager.FindByEmailAsync(buyerEmail);
            if (buyer != null)
            {
                var producerIds = items.Select(i => i.ProducerId).Distinct().ToList();

                foreach (var producerId in producerIds)
                {
                    var producer = await _userManager.FindByIdAsync(producerId);
                    if (producer == null) continue;

                    // FIX: mesajul pentru CUMPARATOR — sender = producer, recipient = buyer
                    // Apare in chat-ul cumparatorului pe DREAPTA (ca si cum producatorul i-a scris)
                    var msgToBuyer = new Message
                    {
                        SenderId = producer.Id,
                        SenderUsername = producer.Email,
                        RecipientId = buyer.Id,
                        RecipientUsername = buyer.Email,
                        Sender = producer,
                        Recipient = buyer,
                        OrderId = order.Id,
                        Content = $"✅ Comanda #{order.Id} a fost plasată cu succes! Vei fi notificat când comanda este expediată."
                    };

                    // FIX: mesajul pentru VANZATOR — sender = buyer, recipient = producer
                    // Apare in chat-ul vanzatorului pe STANGA (ca si cum cumparatorul i-a scris)
                    // IMPORTANT: are OrderId diferit de msgToBuyer? NU — acelasi orderId
                    // Dar sunt mesaje SEPARATE: fiecare user vede doar mesajele unde e sender sau recipient
                    var msgToProducer = new Message
                    {
                        SenderId = buyer.Id,
                        SenderUsername = buyer.Email,
                        RecipientId = producer.Id,
                        RecipientUsername = producer.Email,
                        Sender = buyer,
                        Recipient = producer,
                        OrderId = order.Id,
                        Content = $"🛒 Comandă nouă #{order.Id} de la {buyer.DisplayName}! Intră în comenzile tale pentru detalii."
                    };

                    // Salvam ambele in DB — fiecare user vede doar mesajele lui prin spec
                    _unitOfWork.Repository<Message>().Add(msgToBuyer);
                    _unitOfWork.Repository<Message>().Add(msgToProducer);
                }

                await _unitOfWork.Complete();
            }

            return order;
        }

        public async Task<IReadOnlyList<DeliveryMethod>> GetDeliveryMethodsAsync()
        {
            return await _unitOfWork.Repository<DeliveryMethod>().ListAllAsync();
        }

        public async Task<Order> GetOrderByIdAsync(int id, string buyerEmail)
        {
            var spec = new OrdersWithItemsAndOrderingSpecification(id, buyerEmail);
            return await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec);
        }

        public async Task<IReadOnlyList<Order>> GetOrdersForUserAsync(string buyerEmail)
        {
            var spec = new OrdersWithItemsAndOrderingSpecification(buyerEmail);
            return await _unitOfWork.Repository<Order>().ListAsync(spec);
        }
    }
}