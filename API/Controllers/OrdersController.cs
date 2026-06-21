using System;
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
        private readonly IHubContext<MessageHub> _messageHub;
        private readonly IHubContext<NotificationHub> _notificationHub;
        private readonly IHubContext<PresenceHub> _presenceHub;

        public OrdersController(IOrderService orderService, IMapper mapper,
            IUnitOfWork unitOfWork,
            UserManager<AppUser> userManager,
            IHubContext<MessageHub> messageHub,
            IHubContext<NotificationHub> notificationHub,
            IHubContext<PresenceHub> presenceHub)
        {
            _mapper = mapper;
            _orderService = orderService;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
            _messageHub = messageHub;
            _notificationHub = notificationHub;
            _presenceHub = presenceHub;
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

        [HttpPut("ship-order/{id}")]
        public async Task<ActionResult<OrderToReturnDto>> ShipOrder(int id)
        {
            var spec = new OrdersWithItemsAndOrderingSpecification(id, null);
            var order = await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec);
            if (order == null) return NotFound(new ApiResponse(404));

            if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
                return BadRequest(new ApiResponse(400, "The order has already been shipped."));

            order.Status = OrderStatus.Shipped;
            _unitOfWork.Repository<Order>().Update(order);
            var result = await _unitOfWork.Complete();
            if (result <= 0) return BadRequest(new ApiResponse(400, "Problem updating order status"));

            var producerEmail = User.RetrieveEmailFromPrincipal();
            var producer = await _userManager.FindByEmailAsync(producerEmail);
            var buyer = await _userManager.FindByEmailAsync(order.BuyerEmail);

            if (producer != null &&
                buyer != null &&
                !string.Equals(producer.Email, buyer.Email, StringComparison.OrdinalIgnoreCase))
            {
                var systemMsg = new Message
                {
                    SenderId = producer.Id,
                    SenderUsername = producer.Email,
                    RecipientId = buyer.Id,
                    RecipientUsername = buyer.Email,
                    Sender = producer,
                    Recipient = buyer,
                    OrderId = order.Id,
                    IsSystemMessage = true,
                    Content = $"Platform: Order #{order.Id} has been shipped! You will receive the package soon."
                };

                _unitOfWork.Repository<Message>().Add(systemMsg);
                if (await _unitOfWork.Complete() > 0)
                {
                    await NotifyOrderMessageAsync(systemMsg, producer, buyer);
                }
            }

            return _mapper.Map<Order, OrderToReturnDto>(order);
        }

        [HttpPut("mark-delivered/{id}")]
        public async Task<ActionResult<OrderToReturnDto>> MarkDelivered(int id)
        {
            var buyerEmail = User.RetrieveEmailFromPrincipal();
            var spec = new OrdersWithItemsAndOrderingSpecification(id, buyerEmail);
            var order = await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec);

            if (order == null) return NotFound(new ApiResponse(404));

            if (order.Status != OrderStatus.Shipped && order.Status != OrderStatus.Delivered)
                return BadRequest(new ApiResponse(400, "The order cannot be marked as delivered."));

            var buyer = await _userManager.FindByEmailAsync(buyerEmail);
            var messagesToNotify = new List<(Message Message, AppUser Producer)>();

            if (buyer != null)
            {
                var producerIds = order.OrderItems
                    .Where(item => !string.IsNullOrWhiteSpace(item.ProducerId))
                    .Select(item => item.ProducerId)
                    .Distinct()
                    .ToList();

                foreach (var producerId in producerIds)
                {
                    var producer = await _userManager.FindByIdAsync(producerId);
                    if (producer == null ||
                        string.Equals(producer.Email, buyer.Email, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var promptSpec = new DeliveryReviewPromptMessageSpecification(order.Id, producer.Email, buyer.Email);
                    var existingPromptCount = await _unitOfWork.Repository<Message>().CountAsync(promptSpec);
                    if (existingPromptCount > 0) continue;

                    var systemMsg = new Message
                    {
                        SenderId = producer.Id,
                        SenderUsername = producer.Email,
                        RecipientId = buyer.Id,
                        RecipientUsername = buyer.Email,
                        Sender = producer,
                        Recipient = buyer,
                        OrderId = order.Id,
                        IsSystemMessage = true,
                        IsReviewPrompt = true,
                        Content = $"Platform: {buyer.DisplayName} confirmed delivery for order #{order.Id}. Thank you, please leave a review!"
                    };

                    _unitOfWork.Repository<Message>().Add(systemMsg);
                    messagesToNotify.Add((systemMsg, producer));
                }
            }

            var shouldUpdateStatus = order.Status == OrderStatus.Shipped;
            if (shouldUpdateStatus)
            {
                order.Status = OrderStatus.Delivered;
                _unitOfWork.Repository<Order>().Update(order);
            }

            if (shouldUpdateStatus || messagesToNotify.Count > 0)
            {
                var result = await _unitOfWork.Complete();
                if (result <= 0)
                    return BadRequest(new ApiResponse(400, "Problem confirming order delivery"));
            }

            foreach (var messageToNotify in messagesToNotify)
            {
                await NotifyOrderMessageAsync(messageToNotify.Message, messageToNotify.Producer, buyer);
            }

            return _mapper.Map<Order, OrderToReturnDto>(order);
        }

        private async Task NotifyOrderMessageAsync(Message message, AppUser sender, AppUser recipient)
        {
            var groupName = GetGroupName(sender.Email, recipient.Email) + $"_order_{message.OrderId}";

            await _messageHub.Clients.Group(groupName).SendAsync(
                "NewMessage",
                _mapper.Map<MessageDto>(message));

            var unreadSpec = new UnreadMessagesSpecification(recipient.Email);
            var unreadCount = await _unitOfWork.Repository<Message>().CountAsync(unreadSpec);
            await _notificationHub.Clients.User(recipient.Email).SendAsync("UnreadCount", unreadCount);

            var recipientConnections = await PresenceTracker.GetConnectionsForUser(recipient.Email);
            if (recipientConnections.Any())
            {
                await _presenceHub.Clients.Clients(recipientConnections).SendAsync(
                    "NewMessageReceived",
                    new
                    {
                        senderEmail = sender.Email,
                        senderName = sender.DisplayName,
                        orderId = message.OrderId,
                        isSystemMessage = message.IsSystemMessage,
                        content = message.Content
                    });
            }
        }

        private string GetGroupName(string caller, string other)
        {
            var stringCompare = string.CompareOrdinal(caller, other) < 0;
            return stringCompare ? $"{caller}-{other}" : $"{other}-{caller}";
        }
    }
}
