using CampusTrade.API.Data;
using CampusTrade.API.Models.Entities;
using CampusTrade.API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusTrade.API.Repositories.Implementations
{
    /// <summary>
    /// 分类管理Repository实现
    /// 提供分类层级管理、查询和统计功能
    /// </summary>
    public class CategoriesRepository : Repository<Category>, ICategoriesRepository
    {
        public CategoriesRepository(CampusTradeDbContext context) : base(context)
        {
        }

        // 层级分类查询
        public async Task<IEnumerable<Category>> GetRootCategoriesAsync()
        {
            return await _context.Categories
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetSubCategoriesAsync(int parentId)
        {
            return await _context.Categories
                .Where(c => c.ParentId == parentId)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IEnumerable<Category>> GetCategoryTreeAsync()
        {
            return await _context.Categories
                .Include(c => c.Children)
                .Where(c => c.ParentId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryWithChildrenAsync(int categoryId)
        {
            return await _context.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.CategoryId == categoryId);
        }

        // 分类路径查询
        public async Task<IEnumerable<Category>> GetCategoryPathAsync(int categoryId)
        {
            var categories = new List<Category>();
            var currentCategory = await GetByPrimaryKeyAsync(categoryId);

            while (currentCategory != null)
            {
                categories.Insert(0, currentCategory);
                if (currentCategory.ParentId.HasValue)
                {
                    currentCategory = await GetByPrimaryKeyAsync(currentCategory.ParentId.Value);
                }
                else
                {
                    break;
                }
            }

            return categories;
        }

        public async Task<string> GetCategoryFullNameAsync(int categoryId)
        {
            var path = await GetCategoryPathAsync(categoryId);
            return string.Join(" > ", path.Select(c => c.Name));
        }

        // 分类统计
        public async Task<int> GetProductCountByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .CountAsync(p => p.CategoryId == categoryId);
        }

        public async Task<int> GetActiveProductCountByCategoryAsync(int categoryId)
        {
            return await _context.Products
                .CountAsync(p => p.CategoryId == categoryId && p.Status == "在售");
        }

        public async Task<Dictionary<int, int>> GetCategoryProductCountsAsync()
        {
            return await _context.Categories
                .Select(c => new { c.CategoryId, Count = c.Products.Count })
                .ToDictionaryAsync(x => x.CategoryId, x => x.Count);
        }

        // 分类管理
        public async Task<bool> CanDeleteCategoryAsync(int categoryId)
        {
            // 检查是否有子分类
            var hasChildren = await _context.Categories
                .AnyAsync(c => c.ParentId == categoryId);

            if (hasChildren) return false;

            // 检查是否有商品
            var hasProducts = await _context.Products
                .AnyAsync(p => p.CategoryId == categoryId);

            return !hasProducts;
        }

        public async Task<bool> MoveCategoryAsync(int categoryId, int? newParentId)
        {
            try
            {
                var category = await GetByPrimaryKeyAsync(categoryId);
                if (category == null) return false;

                // 防止循环引用
                if (newParentId.HasValue)
                {
                    var parentPath = await GetCategoryPathAsync(newParentId.Value);
                    if (parentPath.Any(p => p.CategoryId == categoryId))
                        return false;
                }

                category.ParentId = newParentId;
                Update(category);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<Category>> GetCategoriesWithProductsAsync()
        {
            return await _context.Categories
                .Where(c => c.Products.Any())
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        // 搜索功能
        public async Task<IEnumerable<Category>> SearchCategoriesAsync(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return await GetAllAsync();

            return await _context.Categories
                .Where(c => c.Name.Contains(keyword))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetCategoryByNameAsync(string name, int? parentId = null)
        {
            var query = _context.Categories.Where(c => c.Name == name);

            if (parentId.HasValue)
            {
                query = query.Where(c => c.ParentId == parentId);
            }
            else
            {
                query = query.Where(c => c.ParentId == null);
            }

            return await query.FirstOrDefaultAsync();
        }
    }
}