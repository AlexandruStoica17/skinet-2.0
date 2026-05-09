using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using API.Dtos;
using API.Errors;
using API.Extensions;
using AutoMapper;
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
        public OrdersController(IOrderService orderService, IMapper mapper, IUnitOfWork unitOfWork, UserManager<AppUser> userManager)
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
            // 1. Aflăm cine e vânzătorul conectat
            var email = User.FindFirstValue(ClaimTypes.Email);
            var user = await _userManager.FindByEmailAsync(email); // Asigură-te că _userManager e injectat în constructor! Dacă nu e, dă-mi codul fișierului să te ajut.

            // 2. Aducem din baza de date doar comenzile care îl privesc
            var spec = new OrdersForProducerSpecification(user.Id);
            var orders = await _unitOfWork.Repository<Order>().ListAsync(spec);

            // 3. Mapăm datele pentru Angular
            var ordersToReturn = _mapper.Map<IReadOnlyList<Order>, IReadOnlyList<OrderToReturnDto>>(orders);

            // 4. SECURITATE: Ștergem din memorie produsele care nu aparțin acestui vânzător
            // Astfel, nu vede ce altceva a mai comandat clientul de la alte firme!
            foreach (var order in ordersToReturn)
            {
                order.OrderItems = order.OrderItems.Where(i => i.ProducerId == user.Id).ToList();

                // Recalculăm subtotalul doar pentru produsele lui (opțional, dar arată bine vizual)
                order.Subtotal = order.OrderItems.Sum(i => i.Price * i.Quantity);
            }

            return Ok(ordersToReturn);
        }

       [HttpPut("ship-order/{id}")]
public async Task<ActionResult<OrderToReturnDto>> ShipOrder(int id)
{
    // Trebuie să tragem și Metoda de Livrare + Produsele pentru ca AutoMapper să poată calcula Totalul!
    var spec = new OrdersWithItemsAndOrderingSpecification(id, null); // null pentru că nu ne interesează ce user e (suntem pe flux de vânzător)
    var order = await _unitOfWork.Repository<Order>().GetEntityWithSpec(spec);

    if (order == null) return NotFound(new ApiResponse(404));

    // Schimbăm statusul
    order.Status = OrderStatus.Shipped;
    
    // Salvăm modificarea
    _unitOfWork.Repository<Order>().Update(order);
    var result = await _unitOfWork.Complete();

    if (result <= 0) return BadRequest(new ApiResponse(400, "Problem updating order status"));

    // Acum AutoMapper va funcționa pentru că are acces la order.DeliveryMethod.Price!
    return _mapper.Map<Order, OrderToReturnDto>(order);
}

    }
}