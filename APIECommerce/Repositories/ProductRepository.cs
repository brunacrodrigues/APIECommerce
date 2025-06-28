using APIECommerce.Context;
using APIECommerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace APIECommerce.Repositories
{
    public class ProductRepository : IProductRepository
    {

        private readonly AppDbContext _dbContext;


        public ProductRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }


        public async Task<IEnumerable<Product>> GetBestSellerProductsAsync()
        {
            return await _dbContext.Products.AsNoTracking().
                Where(p => p.BestSeller).
                ToListAsync();
        }


        public async Task<IEnumerable<Product>> GetPopularProductsAsync()
        {
            return await _dbContext.Products.AsNoTracking().
                Where(p => p.Popular).                
                ToListAsync();
        }


        public async Task<Product> GetProductDetailsAsync(int id)
        {
            var productDetail = await _dbContext.Products.AsNoTracking().
                FirstOrDefaultAsync(p => p.Id == id);

            return productDetail!;
        }


        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _dbContext.Products.AsNoTracking().
                Where(p => p.CategoryId == categoryId).
                ToListAsync();
        }
    }
}
