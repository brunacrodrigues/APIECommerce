using APIECommerce.Context;
using APIECommerce.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace APIECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly AppDbContext _dbContext;

        public OrdersController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Post([FromBody] Order order)
        {
            order.OrderDate = DateTime.Now;

            var shoppingCartItems = await _dbContext.ShoppingCartItems
                .Where(cart => cart.ClientId == order.UserId)
                .ToListAsync();

            if (shoppingCartItems.Count == 0)
            {
                return NotFound("There are no items in the cart to create the order.");
            }

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    _dbContext.Orders.Add(order);
                    await _dbContext.SaveChangesAsync();

                    foreach (var item in shoppingCartItems)
                    {
                        var orderDetail = new OrderDetail()
                        {
                            Price = item.UnitPrice,
                            Total = item.Total,
                            Quantity = item.Quantity,
                            ProductId = item.ProductId,
                            OrderId = order.Id,
                        };
                        _dbContext.OrderDetails.Add(orderDetail);
                    }

                    await _dbContext.SaveChangesAsync();
                    _dbContext.ShoppingCartItems.RemoveRange(shoppingCartItems);
                    await _dbContext.SaveChangesAsync();

                    await transaction.CommitAsync();

                    return Ok(new { OrderId = order.Id });
                }
                catch (Exception)
                {
                    await transaction.RollbackAsync();
                    return BadRequest("An error occurred while processing the order.");
                }
            }
        }

        // GET: api/Pedidos/PedidosPorUser/5
        // Obtêm todos os pedidos de um user específico com base no UserId.
        [HttpGet("[action]/{userId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrdersByUser(int userId)
        {

            var orders = await _dbContext.Orders
                .AsNoTracking()
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    Id = o.Id,
                    Total = o.Total,
                    OrderDate = o.OrderDate,
                })
                .ToListAsync();

            if (!orders.Any())
            {
                return NotFound("No orders were found for the specified user.");
            }

            return Ok(orders);
        }


        // GET: api/Orders/OrderDetails/5
        // Retorna os detalhes de um pedido específico, incluindo informações sobre
        // os produtos associados a esse pedido.
        [HttpGet("[action]/{orderId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderDetails(int orderId)
        {

            var orderDetails = await _dbContext.OrderDetails.AsNoTracking()
                .Where(od => od.OrderId == orderId)
                .Select(od => new
                {
                    Id = od.Id,
                    Quantity = od.Quantity,
                    SubTotal = od.Total,
                    ProductName = od.Product!.Name,
                    ProductImage = od.Product.UrlImage,
                    Price = od.Product.Price
                })
                .ToListAsync();

            if (!orderDetails.Any())
            {
                return NotFound("Order details not found.");
            }

            return Ok(orderDetails);
        }
    }
}
 
