using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Dtos;
using API.Errors;
using API.Extensions;
using API.SignalR;
using AutoMapper;
using Core.Entities;
using Core.Entities.Identity;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace API.Controllers
{
    [Authorize]
    public class OrdersController : BaseApiController
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;
        private readonly IHubContext<MessageHub> _messageHub; // NOU: pentru mesaje automate in timp real

        public OrdersController(
            IOrderService orderService,
            IMapper mapper,
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IHubContext<MessageHub> messageHub) // NOU
        {
            _mapper = mapper;
            _orderService = orderService;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _messageHub = messageHub;
        }

        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(OrderDto orderDto)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            var address = _mapper.Map<AddressDto, Core.Entities.OrderAggregate.Address>(orderDto.ShipToAddress);
            var order = await _orderService.CreateOrderAsync(email, orderDto.DeliveryMethodId, orderDto.BasketId, address);
            if (order == null) return BadRequest(new ApiResponse(400, "Problem creating order"));
            return Ok(order);
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<OrderToReturnDto>>> GetOrdersForUser()
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            var orders = await _orderService.GetOrdersForUserAsync(email);
            return Ok(_mapper.Map<IReadOnlyList<OrderToReturnDto>>(orders));
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderToReturnDto>> GetOrderByIdForUser(int id)
        {
            var email = HttpContext.User.RetrieveEmailFromPrincipal();
            var order = await _orderService.GetOrderByIdAsync(id, email);
            if (order == null) return NotFound(new ApiResponse(404));
            return _mapper.Map<OrderToReturnDto>(order);
        }

        [HttpGet("deliveryMethods")]
        public async Task<ActionResult<IReadOnlyList<DeliveryMethod>>> GetDeliveryMethods()
        {
            return Ok(await _orderService.GetDeliveryMethodsAsync());
        }

        [Authorize(Roles = "CosmeticsProducer,IngredientsProducer")]
        [HttpGet("producer-orders")]
        public async Task<ActionResult<IReadOnlyList<OrderToReturnDto>>> GetProducerOrders()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email);

            var spec = new OrdersForProducerSpecification(user.Id);
            var orders = await _unitOfWork.Repository<Order>().ListAsync(spec);

            var ordersToReturn = _mapper.Map<IReadOnlyList<Order>, IReadOnlyList<OrderToReturnDto>>(orders);

            foreach (var order in ordersToReturn)
            {
                order.OrderItems = order.OrderItems.Where(i => i.ProducerId == user.Id).ToList();
                order.Subtotal = order.OrderItems.Sum(i => i.Price * i.Quantity);
            }

            return Ok(ordersToReturn);
        }

        // Vanzatorul marcheaza comanda ca expediata + trimite mesaj automat in chat
        [HttpPut("ship-order/{id}")]
        public async Task<ActionResult<OrderToReturnDto>> ShipOrder(int id)
        {
            var spec = new OrdersWithItemsAndOrderingSpecification(id, null);
            var order = await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec);

            if (order == null) return NotFound(new ApiResponse(404));

            // NOU: schimbam statusul
            order.Status = OrderStatus.Shipped;
            _unitOfWork.Repository<Order>().Update(order);
            var result = await _unitOfWork.Complete();

            if (result <= 0) return BadRequest(new ApiResponse(400, "Problem updating order status"));

            // NOU: Trimitem mesaj automat in chat catre cumparator
            var producerEmail = User.RetrieveEmailFromPrincipal();
            var producer = await _userManager.FindByEmailAsync(producerEmail);
            var buyer = await _userManager.FindByEmailAsync(order.BuyerEmail);

            if (producer != null && buyer != null)
            {
                var msg = new Message
                {
                    SenderId = producer.Id,
                    SenderUsername = producer.Email,
                    RecipientId = buyer.Id,
                    RecipientUsername = buyer.Email,
                    Sender = producer,
                    Recipient = buyer,
                    Content = $"🚚 Comanda #{order.Id} a fost expediată! Vei primi coletul în curând."
                };
                _unitOfWork.Repository<Message>().Add(msg);
                await _unitOfWork.Complete();

                // Trimitem mesajul in timp real daca cumparatorul e in conversatie
                var groupName = GetGroupName(producer.Email, buyer.Email);
                await _messageHub.Clients.Group(groupName).SendAsync("NewMessage",
                    new MessageDto
                    {
                        Id = msg.Id,
                        SenderId = msg.SenderId,
                        SenderUsername = msg.SenderUsername,
                        RecipientId = msg.RecipientId,
                        RecipientUsername = msg.RecipientUsername,
                        Content = msg.Content,
                        MessageSent = msg.MessageSent
                    });
            }

            return _mapper.Map<Order, OrderToReturnDto>(order);
        }

        // NOU: Cumparatorul confirma ca a primit comanda + mesaj automat + prompt review
        [HttpPut("mark-delivered/{id}")]
        public async Task<ActionResult<OrderToReturnDto>> MarkDelivered(int id)
        {
            var buyerEmail = User.RetrieveEmailFromPrincipal();
            var order = await _orderService.GetOrderByIdAsync(id, buyerEmail);

            if (order == null) return NotFound(new ApiResponse(404));
            if (order.Status != OrderStatus.Shipped)
                return BadRequest(new ApiResponse(400, "Comanda nu este in status Shipped."));

            // Schimbam statusul
            order.Status = OrderStatus.Delivered;
            _unitOfWork.Repository<Order>().Update(order);
            await _unitOfWork.Complete();

            // NOU: Trimitem mesaj automat la ambii participanti
            var buyer = await _userManager.FindByEmailAsync(buyerEmail);
            var firstItem = order.OrderItems.FirstOrDefault();
            if (buyer != null && firstItem != null)
            {
                var producer = await _userManager.FindByIdAsync(firstItem.ProducerId);
                if (producer != null)
                {
                    // Mesaj catre VANZATOR
                    var msgToProducer = new Message
                    {
                        SenderId = buyer.Id,
                        SenderUsername = buyer.Email,
                        RecipientId = producer.Id,
                        RecipientUsername = producer.Email,
                        Sender = buyer,
                        Recipient = producer,
                        Content = $"✅ {buyer.DisplayName} a confirmat primirea comenzii #{order.Id}."
                    };

                    // Mesaj catre CUMPARATOR: invitatie review
                    var msgToBuyer = new Message
                    {
                        SenderId = producer.Id,
                        SenderUsername = producer.Email,
                        RecipientId = buyer.Id,
                        RecipientUsername = buyer.Email,
                        Sender = producer,
                        Recipient = buyer,
                        Content = $"⭐ Multumim pentru comanda #{order.Id}! Lasa un review pentru a ajuta alti cumparatori.",
                        // NOU: marcam mesajul ca review prompt
                        IsReviewPrompt = true,
                        OrderId = order.Id
                    };

                    _unitOfWork.Repository<Message>().Add(msgToProducer);
                    _unitOfWork.Repository<Message>().Add(msgToBuyer);
                    await _unitOfWork.Complete();

                    // Trimitem in timp real
                    var groupName = GetGroupName(producer.Email, buyer.Email);
                    await _messageHub.Clients.Group(groupName).SendAsync("NewMessage",
                        new MessageDto
                        {
                            Id = msgToBuyer.Id,
                            SenderId = msgToBuyer.SenderId,
                            SenderUsername = msgToBuyer.SenderUsername,
                            RecipientId = msgToBuyer.RecipientId,
                            RecipientUsername = msgToBuyer.RecipientUsername,
                            Content = msgToBuyer.Content,
                            MessageSent = msgToBuyer.MessageSent,
                            IsReviewPrompt = true,
                            OrderId = order.Id
                        });
                }
            }

            return _mapper.Map<Order, OrderToReturnDto>(order);
        }

        // Helper: acelasi GetGroupName ca in MessageHub
        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}