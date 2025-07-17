using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusTrade.API.Models.Entities
{
    /// <summary>
    /// 商品分类实体类 - 对应 Oracle 数据库中的 CATEGORIES 表
    /// 支持树形结构，可以有父分类和子分类
    /// </summary>
    [Table("CATEGORIES")]
    public class Category
    {
        /// <summary>
        /// 分类ID - 主键，对应Oracle中的category_id字段，非自增，Oracle序列和触发器处理
        /// </summary>
        [Key]
        [Column("CATEGORY_ID")]
        public int CategoryId { get; set; }

        /// <summary>
        /// 父分类ID - 外键，对应Oracle中的parent_id字段
        /// 如果为空，则表示这是一级分类（根分类）
        /// </summary>
        [Column("PARENT_ID")]
        public int? ParentId { get; set; }

        /// <summary>
        /// 分类名称 - 对应Oracle中的name字段
        /// 分类的显示名称，最大长度50字符
        /// </summary>
        [Required]
        [Column("NAME", TypeName = "VARCHAR2(50)")]
        [StringLength(50, ErrorMessage = "分类名称不能超过50个字符")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 父分类 - 多对一关系
        /// 通过ParentId外键关联到父分类
        /// </summary>
        [ForeignKey("ParentId")]
        public virtual Category? Parent { get; set; }

        /// <summary>
        /// 子分类集合 - 一对多关系
        /// 当前分类下的所有子分类
        /// </summary>
        public virtual ICollection<Category> Children { get; set; } = new List<Category>();

        /// <summary>
        /// 该分类下的商品集合 - 一对多关系
        /// 属于该分类的所有商品
        /// </summary>
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();

        /// <summary>
        /// 负责管理该分类的管理员集合 - 一对多关系
        /// 分配给该分类的分类管理员
        /// </summary>
        public virtual ICollection<Admin> Admins { get; set; } = new List<Admin>();

        /// <summary>
        /// 是否为根分类（一级分类）
        /// </summary>
        [NotMapped]
        public bool IsRoot => ParentId == null;

        /// <summary>
        /// 是否为叶子分类（没有子分类）
        /// </summary>
        [NotMapped]
        public bool IsLeaf => !Children.Any();

        // TODO：迁移到Repository层中
        /// <summary>
        /// 分类层级深度（从根分类开始计算）
        /// </summary>
        [NotMapped]
        public int Level
        {
            get
            {
                int level = 0;
                var current = Parent;
                while (current != null)
                {
                    level++;
                    current = current.Parent;
                }
                return level;
            }
        }

        // TODO：迁移到Repository层中
        /// <summary>
        /// 完整路径名称（从根分类到当前分类）
        /// 例如：教材 > 计算机类 > 急出
        /// </summary>
        [NotMapped]
        public string FullPath
        {
            get
            {
                var paths = new List<string>();
                var current = this;

                while (current != null)
                {
                    paths.Insert(0, current.Name);
                    current = current.Parent;
                }

                return string.Join(" > ", paths);
            }
        }

        // TODO：迁移到Repository层中
        /// <summary>
        /// 检查是否可以将指定分类设置为父分类
        /// 防止循环引用
        /// </summary>
        /// <param name="potentialParentId">潜在父分类ID</param>
        /// <returns>是否可以设置</returns>
        public bool CanSetParent(int potentialParentId)
        {
            // 不能将自己设为父分类
            if (potentialParentId == CategoryId)
                return false;

            // 不能将自己的后代设为父分类（需要检查已加载的Children）
            var descendantIds = GetLoadedDescendants().Select(d => d.CategoryId).ToList();
            return !descendantIds.Contains(potentialParentId);
        }

        // TODO：迁移到Repository层中
        /// <summary>
        /// 获取已加载的后代分类（仅当前内存中的Children）
        /// </summary>
        /// <returns>已加载的后代分类列表</returns>
        private List<Category> GetLoadedDescendants()
        {
            var descendants = new List<Category>();

            foreach (var child in Children)
            {
                descendants.Add(child);
                descendants.AddRange(child.GetLoadedDescendants());
            }

            return descendants;
        }

        // TODO：迁移到Repository层中
        /// <summary>
        /// 检查分类名称在同级分类中是否唯一（需要提供同级分类数据）
        /// </summary>
        /// <param name="siblings">同级分类列表</param>
        /// <returns>是否唯一</returns>
        public bool IsNameUniqueInSiblings(IEnumerable<Category> siblings)
        {
            return !siblings.Any(s => s.CategoryId != CategoryId &&
                                     s.Name.Equals(Name, StringComparison.OrdinalIgnoreCase));
        }

        // TODO：迁移到Repository层中
        /// <summary>
        /// 获取分类的显示名称（包含层级缩进）
        /// </summary>
        /// <param name="indentChar">缩进字符</param>
        /// <returns>带缩进的显示名称</returns>
        public string GetDisplayName(string indentChar = "  ")
        {
            return new string(' ', Level * indentChar.Length) + Name;
        }

        // TODO：迁移到Repository层中
        /// <summary>
        /// 重写ToString方法，返回完整路径
        /// </summary>
        /// <returns>分类的完整路径</returns>
        public override string ToString()
        {
            return FullPath;
        }
    }
}
