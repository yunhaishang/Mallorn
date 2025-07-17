using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// 分类管理Repository接口
    /// 提供分类层级管理、查询和统计功能
    /// </summary>
    public interface ICategoriesRepository : IRepository<Category>
    {
        // 层级分类查询
        Task<IEnumerable<Category>> GetRootCategoriesAsync();
        Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId);
        Task<IEnumerable<Category>> GetCategoryTreeAsync();
        Task<Category?> GetCategoryWithChildrenAsync(int categoryId);

        // 分类路径查询
        Task<IEnumerable<Category>> GetCategoryPathAsync(int categoryId);
        Task<string> GetCategoryFullNameAsync(int categoryId);

        // 分类统计
        Task<int> GetProductCountByCategoryAsync(int categoryId);
        Task<int> GetActiveProductCountByCategoryAsync(int categoryId);
        Task<Dictionary<int, int>> GetCategoryProductCountsAsync();

        // 分类管理
        Task<bool> CanDeleteCategoryAsync(int categoryId);
        Task<bool> MoveCategoryAsync(int categoryId, int? newParentId);
        Task<IEnumerable<Category>> GetCategoriesWithProductsAsync();

        // 搜索功能
        Task<IEnumerable<Category>> SearchCategoriesAsync(string keyword);
        Task<Category?> GetCategoryByNameAsync(string name, int? parentId = null);
    }
} 