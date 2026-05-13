using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Dtos;
using API.Errors;
using API.Extensions;
using AutoMapper;
using Core.Entities;
using Core.Entities.Identity;
using Core.Entities.OrderAggregate;
using Core.Interfaces;
using Core.Specifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class OrdersController : BaseApiController
    {
        private readonly IOrderService _orderService;
        private readonly IMapper _mapper;
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<AppUser> _userManager;

        public OrdersController(IOrderService orderService, IMapper mapper,
            IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
        {
            _mapper = mapper;
            _orderService = orderService;
            _unitOfWork = unitOfWork;
            _userManager = userManager;
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

            order.Status = OrderStatus.Shipped;
            _unitOfWork.Repository<Order>().Update(order);
            var result = await _unitOfWork.Complete();

            if (result <= 0) return BadRequest(new ApiResponse(400, "Problem updating order status"));

            // Mesaj automat cu OrderId in chat-ul comenzii
            var producerEmail = User.RetrieveEmailFromPrincipal();
            var producer = await _userManager.FindByEmailAsync(producerEmail);
            var buyer = await _userManager.FindByEmailAsync(order.BuyerEmail);

            if (producer != null && buyer != null)
            {
                // Mesaj catre cumparator: expediat
                var msgToBuyer = new Message
                {
                    SenderId = producer.Id,
                    SenderUsername = producer.Email,
                    RecipientId = buyer.Id,
                    RecipientUsername = buyer.Email,
                    Sender = producer,
                    Recipient = buyer,
                    OrderId = order.Id,
                    Content = $"🚚 Comanda #{order.Id} a fost expediată! Vei primi coletul în curând."
                };

                // Mesaj catre vanzator: confirmare
                var msgToProducer = new Message
                {
                    SenderId = buyer.Id,
                    SenderUsername = buyer.Email,
                    RecipientId = producer.Id,
                    RecipientUsername = producer.Email,
                    Sender = buyer,
                    Recipient = producer,
                    OrderId = order.Id,
                    Content = $"✅ Ai marcat comanda #{order.Id} ca expediată."
                };

                _unitOfWork.Repository<Message>().Add(msgToBuyer);
                _unitOfWork.Repository<Message>().Add(msgToProducer);
                await _unitOfWork.Complete();
            }

            return _mapper.Map<Order, OrderToReturnDto>(order);
        }

        // NOU: cumparatorul confirma ca a primit comanda
        [HttpPut("mark-delivered/{id}")]
        public async Task<ActionResult<OrderToReturnDto>> MarkDelivered(int id)
        {
            var buyerEmail = User.RetrieveEmailFromPrincipal();

            // Aducem comanda fara restrictie de email (cumparatorul o poate accesa)
            var spec = new OrdersWithItemsAndOrderingSpecification(id, buyerEmail);
            var order = await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec);

            if (order == null) return NotFound(new ApiResponse(404));

            order.Status = OrderStatus.Delivered;
            _unitOfWork.Repository<Order>().Update(order);
            await _unitOfWork.Complete();

            var buyer = await _userManager.FindByEmailAsync(buyerEmail);
            var firstItem = order.OrderItems.FirstOrDefault();

            if (buyer != null && firstItem != null)
            {
                var producer = await _userManager.FindByIdAsync(firstItem.ProducerId);
                if (producer != null)
                {
                    // Mesaj catre vanzator: comanda primita
                    var msgToProducer = new Message
                    {
                        SenderId = buyer.Id,
                        SenderUsername = buyer.Email,
                        RecipientId = producer.Id,
                        RecipientUsername = producer.Email,
                        Sender = buyer,
                        Recipient = producer,
                        OrderId = order.Id,
                        Content = $"✅ {buyer.DisplayName} a confirmat primirea comenzii #{order.Id}."
                    };

                    // Mesaj catre cumparator: invitatie review
                    var msgToBuyer = new Message
                    {
                        SenderId = producer.Id,
                        SenderUsername = producer.Email,
                        RecipientId = buyer.Id,
                        RecipientUsername = buyer.Email,
                        Sender = producer,
                        Recipient = buyer,
                        OrderId = order.Id,
                        IsReviewPrompt = true,
                        Content = $"⭐ Mulțumim pentru comanda #{order.Id}! Lasă un review pentru a ajuta alți cumpărători."
                    };

                    _unitOfWork.Repository<Message>().Add(msgToProducer);
                    _unitOfWork.Repository<Message>().Add(msgToBuyer);
                    await _unitOfWork.Complete();
                }
            }

            return _mapper.Map<Order, OrderToReturnDto>(order);
        }
    }
}