using APIECommerce.Entities;

namespace APIECommerce.Repositories
{
    public interface ICategoryRepository
    {
        Task<IEnumerable<Category>> GetCategories();
    }
}
