using CampusTrade.API.Models.Entities;

namespace CampusTrade.API.Repositories.Interfaces
{
    /// <summary>
    /// Product实体的Repository接口
    /// 继承基础IRepository，提供Product特有的查询和操作方法
    /// </summary>
    public interface IProductRepository : IRepository<Product>
    {
        // 商品查询
        Task<(IEnumerable<Product> Products, int TotalCount)> GetByUserIdAsync(int userId);
        Task<(IEnumerable<Product> Products, int TotalCount)> GetByCategoryIdAsync(int categoryId);
        Task<(IEnumerable<Product> Products, int TotalCount)> GetByTitleAsync(string title);
        Task<bool> IsProductExistsAsync(string title, int userId);

        // 商品状态管理
        // TODO：目前没有加数量，这个函数的实现需要加上商品数量变化，这个函数在系统修改时调用
        Task<Product> SetProductStatusAsync(int productId, string status);
        // TODO：这个函数也要加数量属性的变化，这个函数在用户自己修改时调用
        Task<Product?> UpdateProductDetailsAsync(
            int productId,
            string? title = null,
            string? description = null,
            decimal? basePrice = null
        );

        // 关系查询
        Task<Product?> GetProductWithOrdersAsync(int productId);


        // 商品数据统计相关
        Task<int> GetTotalProductsNumberAsync();
        Task<IEnumerable<Product>> GetTopViewProductsAsync(int count);

        // 扩展查询方法
        Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
            int pageIndex, 
            int pageSize, 
            int? categoryId = null, 
            string? status = null, 
            string? keyword = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? userId = null);

        Task IncreaseViewCountAsync(int productId);
        Task<IEnumerable<Product>> GetAutoRemoveProductsAsync(DateTime beforeTime);
        Task<IEnumerable<string>> GetProductImagesAsync(int productId);
        Task<bool> DeleteProductAsync(int productId);
        Task<int> DeleteProductsByUserAsync(int userId);
    }
}