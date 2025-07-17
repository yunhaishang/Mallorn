using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// Product实体的Repository实现类
    /// 继承基础Repository，提供Product特有的查询和操作方法
    /// </summary>
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(CampusTradeDbContext context) : base(context)
        {
        }

        #region 商品查询

        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetByUserIdAsync(int userId)
        {
            var query = _dbSet.Where(p => p.UserId == userId);
            var totalCount = await query.CountAsync();
            var products = await query
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.PublishTime)
                .ToListAsync();

            return (products, totalCount);
        }

        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetByCategoryIdAsync(int categoryId)
        {
            var query = _dbSet.Where(p => p.CategoryId == categoryId && p.Status == Product.ProductStatus.OnSale);
            var totalCount = await query.CountAsync();
            var products = await query
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.PublishTime)
                .ToListAsync();

            return (products, totalCount);
        }

        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetByTitleAsync(string title)
        {
            var query = _dbSet.Where(p => p.Title.Contains(title) && p.Status == Product.ProductStatus.OnSale);
            var totalCount = await query.CountAsync();
            var products = await query
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.PublishTime)
                .ToListAsync();

            return (products, totalCount);
        }

        public async Task<bool> IsProductExistsAsync(string title, int userId)
        {
            return await _dbSet.AnyAsync(p => p.Title == title && p.UserId == userId);
        }

        #endregion

        #region 商品状态管理

        public async Task<Product> SetProductStatusAsync(int productId, string status)
        {
            var product = await GetByPrimaryKeyAsync(productId);
            if (product == null)
                throw new ArgumentException($"商品ID {productId} 不存在");

            if (!Product.IsValidStatus(status))
                throw new ArgumentException($"无效的商品状态: {status}");

            product.UpdateStatus(status);
            Update(product);
            return product;
        }

        public async Task<Product?> UpdateProductDetailsAsync(
            int productId,
            string? title = null,
            string? description = null,
            decimal? basePrice = null)
        {
            var product = await GetByPrimaryKeyAsync(productId);
            if (product == null)
                return null;

            if (!string.IsNullOrEmpty(title))
                product.Title = title;
            if (!string.IsNullOrEmpty(description))
                product.Description = description;
            if (basePrice.HasValue)
                product.BasePrice = basePrice.Value;

            Update(product);
            return product;
        }

        #endregion

        #region 关系查询

        public async Task<Product?> GetProductWithOrdersAsync(int productId)
        {
            return await _dbSet
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        #endregion

        #region 商品数据统计相关

        public async Task<int> GetTotalProductsNumberAsync()
        {
            return await _dbSet.CountAsync();
        }

        public async Task<IEnumerable<Product>> GetTopViewProductsAsync(int count)
        {
            return await _dbSet
                .Where(p => p.Status == Product.ProductStatus.OnSale)
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.ViewCount)
                .Take(count)
                .ToListAsync();
        }

        #endregion

        #region 扩展商品查询方法

        /// <summary>
        /// 分页多条件查询商品
        /// </summary>
        public async Task<(IEnumerable<Product> Products, int TotalCount)> GetPagedProductsAsync(
            int pageIndex,
            int pageSize,
            int? categoryId = null,
            string? status = null,
            string? keyword = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            int? userId = null)
        {
            var query = _dbSet.AsQueryable();

            // 应用过滤条件
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p => p.Title.Contains(keyword) || p.Description!.Contains(keyword));

            if (minPrice.HasValue)
                query = query.Where(p => p.BasePrice >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.BasePrice <= maxPrice.Value);

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId.Value);

            var totalCount = await query.CountAsync();
            var products = await query
                .Include(p => p.User)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .OrderByDescending(p => p.PublishTime)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (products, totalCount);
        }

        /// <summary>
        /// 增加商品浏览量
        /// </summary>
        public async Task IncreaseViewCountAsync(int productId)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "UPDATE PRODUCTS SET VIEW_COUNT = VIEW_COUNT + 1 WHERE PRODUCT_ID = {0}",
                productId);
        }

        /// <summary>
        /// 获取即将自动下架的商品
        /// </summary>
        public async Task<IEnumerable<Product>> GetAutoRemoveProductsAsync(DateTime beforeTime)
        {
            return await _dbSet
                .Where(p => p.AutoRemoveTime.HasValue && p.AutoRemoveTime.Value <= beforeTime
                           && p.Status == Product.ProductStatus.OnSale)
                .Include(p => p.User)
                .ToListAsync();
        }

        /// <summary>
        /// 获取商品图片
        /// </summary>
        public async Task<IEnumerable<string>> GetProductImagesAsync(int productId)
        {
            return await _context.Set<ProductImage>()
                .Where(pi => pi.ProductId == productId)
                .Select(pi => pi.ImageUrl)
                .ToListAsync();
        }

        /// <summary>
        /// 逻辑删除商品
        /// </summary>
        public async Task<bool> DeleteProductAsync(int productId)
        {
            var product = await GetByPrimaryKeyAsync(productId);
            if (product == null)
                return false;

            product.UpdateStatus(Product.ProductStatus.OffShelf);
            Update(product);
            return true;
        }

        /// <summary>
        /// 批量删除用户的商品
        /// </summary>
        public async Task<int> DeleteProductsByUserAsync(int userId)
        {
            var products = await _dbSet.Where(p => p.UserId == userId).ToListAsync();
            foreach (var product in products)
            {
                product.UpdateStatus(Product.ProductStatus.OffShelf);
            }
            UpdateRange(products);
            return products.Count;
        }

        #endregion
    }
}