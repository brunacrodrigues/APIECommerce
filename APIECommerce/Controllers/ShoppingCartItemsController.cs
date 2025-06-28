using APIECommerce.Context;
using APIECommerce.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace APIECommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ShoppingCartItemsController : ControllerBase
    {
        private readonly AppDbContext _dbContext;


        public ShoppingCartItemsController(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        // GET: api/ShoppingCartItems/1
        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(int userId)
        {
            var user = await _dbContext.Users.FindAsync(userId);

            if (user is null)
            {
                return NotFound($"The user with id = {userId} was not found");
            }

            var shoppingCartItems = await (from s in _dbContext.ShoppingCartItems.Where(s => s.ClientId == userId)
                                           join p in _dbContext.Products on s.ProductId equals p.Id
                                           select new
                                           {
                                               Id = s.Id,
                                               UnitPrice = p.Price,
                                               // Price = s.UnitPrice,
                                               Total = s.Total,
                                               Quantity = s.Quantity,
                                               ProductId = p.Id,
                                               ProductName = p.Name,
                                               UrlImage = p.UrlImage
                                           }).ToListAsync();

            return Ok(shoppingCartItems);
        }


        [HttpPost]
        public async Task<IActionResult> Post([FromBody] ShoppingCartItem shoppingCartItem)
        {
            try
            {
                var shoppingCart = await _dbContext.ShoppingCartItems.FirstOrDefaultAsync(s =>
                s.ProductId == shoppingCartItem.ProductId &&
                s.ClientId == shoppingCartItem.ClientId);

                if (shoppingCart != null)
                {
                    shoppingCart.Quantity += shoppingCartItem.Quantity;
                    shoppingCart.Total = shoppingCart.UnitPrice * shoppingCart.Quantity;
                }
                else
                {
                    var product = await _dbContext.Products.FindAsync(shoppingCartItem.ProductId);

                    var cart = new ShoppingCartItem()
                    {
                        ClientId = shoppingCartItem.ClientId,
                        ProductId = shoppingCartItem.ProductId,
                        UnitPrice = shoppingCartItem.UnitPrice,
                        Quantity = shoppingCartItem.Quantity,
                        Total = (product!.Price) * (shoppingCartItem.Quantity)
                    };

                    _dbContext.ShoppingCartItems.Add(cart);
                }

                await _dbContext.SaveChangesAsync();
                return StatusCode(StatusCodes.Status201Created);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the request");
            }
        }



        // PUT /api/ShoppingCartItems?produtoId = 1 & acao = "aumentar"
        // PUT /api/ShoppingCartItems?produtoId = 1 & acao = "diminuir"
        // PUT /api/ShoppingCartItems?produtoId = 1 & acao = "apagar"
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Put(int productId, string action)
        {
            // Este codigo recupera o endereço de e-mail do user autenticado do token JWT decodificado,
            // Claims representa as declarações associadas ao user autenticado
            // Assim somente os users autenticados poderão aceder a este endpoint
            var userEmail = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == userEmail);

            if (user is null)
            {
                return NotFound("User not found.");
            }

            var shoppingCartItem = await _dbContext.ShoppingCartItems.FirstOrDefaultAsync(s =>
                                                   s.ClientId == user!.Id && s.ProductId == productId);

            if (shoppingCartItem != null)
            {
                if (action.ToLower() == "aumentar")
                {
                    shoppingCartItem.Quantity += 1;
                }
                else if (action.ToLower() == "diminuir")
                {
                    if (shoppingCartItem.Quantity > 1)
                    {
                        shoppingCartItem.Quantity -= 1;
                    }
                    else
                    {
                        _dbContext.ShoppingCartItems.Remove(shoppingCartItem);
                        await _dbContext.SaveChangesAsync();
                        return Ok();
                    }
                }
                else if (action.ToLower() == "apagar")
                {
                    // Remove o item do carrinho
                    _dbContext.ShoppingCartItems.Remove(shoppingCartItem);
                    await _dbContext.SaveChangesAsync();
                    return Ok();
                }
                else
                {
                    return BadRequest("Invalid action. Use: 'aumentar', 'diminuir', or 'apagar' to perform an action");
                }

                shoppingCartItem.Total = shoppingCartItem.UnitPrice * shoppingCartItem.Quantity;
                await _dbContext.SaveChangesAsync();
                return Ok($"Operation: {action} completed successfully");
            }
            else
            {
                return NotFound("No item found in the cart");

            }
            
        }
               
    }
}

