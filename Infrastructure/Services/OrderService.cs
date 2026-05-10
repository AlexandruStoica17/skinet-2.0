using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Identity;
using Core.Entities.Identity;

// Alias pentru a evita ambiguitate intre cele doua clase Address
using ShippingAddress = Core.Entities.OrderAggregate.Address;

namespace Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly IBasketRepository _basketRepo;
        private readonly IUnitOfWork _unitOfWork;
        // NOU: pentru a gasi userul vanzator si a trimite mesaje automate
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

            // NOU: Trimitem mesaje automate cu OrderId — fiecare comanda = conversatie separata
            var buyer = await _userManager.FindByEmailAsync(buyerEmail);
            if (buyer != null)
            {
                // Grupam produsele dupa producator (poate fi mai multi in acelasi cos)
                var producerIds = items.Select(i => i.ProducerId).Distinct().ToList();

                foreach (var producerId in producerIds)
                {
                    var producer = await _userManager.FindByIdAsync(producerId);
                    if (producer == null) continue;

                    // Mesaj catre CUMPARATOR: confirmare comanda, cu OrderId
                    var msgToBuyer = new Message
                    {
                        SenderId = producer.Id,
                        SenderUsername = producer.Email,
                        RecipientId = buyer.Id,
                        RecipientUsername = buyer.Email,
                        Sender = producer,
                        Recipient = buyer,
                        // NOU: OrderId leaga mesajul de aceasta comanda specifica
                        OrderId = order.Id,
                        Content = $"✅ Comanda #{order.Id} a fost plasată cu succes! Vei fi notificat când comanda este expediată."
                    };

                    // Mesaj catre VANZATOR: notificare comanda noua, cu OrderId
                    var msgToProducer = new Message
                    {
                        SenderId = buyer.Id,
                        SenderUsername = buyer.Email,
                        RecipientId = producer.Id,
                        RecipientUsername = producer.Email,
                        Sender = buyer,
                        Recipient = producer,
                        // NOU: acelasi OrderId
                        OrderId = order.Id,
                        Content = $"🛒 Comandă nouă #{order.Id} de la {buyer.DisplayName}! Intră în comenzile tale pentru detalii."
                    };

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