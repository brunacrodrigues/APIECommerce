using APIECommerce.Context;
using APIECommerce.Entities;
using Microsoft.EntityFrameworkCore;

namespace APIECommerce.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly AppDbContext _dbContext;

        public CategoryRepository(AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Category>> GetCategories()
        {
            return await _dbContext.Categories.AsNoTracking().ToListAsync();
        }
    }
}
