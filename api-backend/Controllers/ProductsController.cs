using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using api_backend.Models;
using api_backend.ViewModels;

namespace api_backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly TeamDb2Context _context;

        public ProductsController(TeamDb2Context context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            return await _context.Products.ToListAsync();
        }

        [HttpGet("withImage")]
        public async Task<ActionResult<IEnumerable<ProductWithImageViewModel>>> GetProductsWithImage()
        {
            var products = await _context.Products
                .Select(p => new ProductWithImageViewModel
                {
                    ProductsId = p.ProductsId,
                    Name = p.Name,
                    ImageUrl = _context.ProductImages
                        .Where(img => img.ProductId == p.ProductsId && img.IsMain == 1)
                        .Select(img => img.ImageUrl.StartsWith("/ProductImages/")
                            ? img.ImageUrl
                            : "/ProductImages/" + img.ImageUrl)
                        .FirstOrDefault()
                })
                .ToListAsync();

            return products;
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);

            if (product == null)
            {
                return NotFound();
            }

            return product;
        }

        // 加在 ProductsController 裡
        [HttpGet("Details/{id}")]
        public IActionResult GetProductDetail(int id)
        {
            var product = _context.Products
                .Where(p => p.ProductsId == id)
                .Select(p => new ProductDetailViewModel
                {
                    ProductsId = p.ProductsId,
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    Status = p.Status,
                    StatusText = p.Status == 1 ? "上架中" : "下架中",
                    CategoryName = p.Category.Name,
                    SortName = p.Sort.Name,
                    ImageUrl = "/ProductImages/" +
                        p.ProductImages
                         .Where(img => img.IsMain == 1)
                         .Select(img => img.ImageUrl)
                         .FirstOrDefault()
                })
                .FirstOrDefault();

            if (product == null)
            {
                return NotFound();
            }

            return Ok(product);
        }

        //給ProductList用
        [HttpGet("filter")]
        public IActionResult FilterProducts(
            int? categoryId,
            int? sortId,
            int? minPrice,
            int? maxPrice)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Sort)
                .Include(p => p.ProductImages)
                .Where(p => p.IsDeleted == 0 && p.Status == 1) // 只抓「未刪除」且「上架中」商品
                .AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            if (sortId.HasValue)
                query = query.Where(p => p.SortId == sortId.Value);

            if (minPrice.HasValue)
                query = query.Where(p => p.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                query = query.Where(p => p.Price <= maxPrice.Value);

            var result = query.Select(p => new
            {
                p.ProductsId,
                p.Name,
                p.Description,
                p.Price,
                p.Stock,
                p.Status,
                StatusText = p.Status == 1 ? "上架中" : "已下架",
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                SortName = p.Sort.Name,
                ImageUrl = p.ProductImages.FirstOrDefault(i => i.IsMain == 1).ImageUrl
            });

            return Ok(result);
        }

        // GET: api/Products/special 特殊款泡麵的api
        [HttpGet("special")]
        public IActionResult GetSpecialNoodles()
        {
            var products = _context.Products
                .Where(p => p.CategoryId == 9)
                .Select(p => new
                {
                    p.ProductsId,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.CategoryId,
                    CategoryName = p.Category.Name,
                    SortName = p.Sort.Name,
                    ImageUrl = p.ProductImages
                                .Where(i => i.IsMain == 1)
                                .Select(i => i.ImageUrl)
                                .FirstOrDefault()
                })
                .ToList();

            return Ok(products);
        }

        // GET: api/Products/recommend?excludeId=73&count=3
        [HttpGet("recommend")]
        public IActionResult GetRecommendedProducts(int excludeId, int count = 3)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Sort)
                .Include(p => p.ProductImages)
                .Where(p => p.ProductsId != excludeId && p.Status == 1 && p.IsDeleted == 0)
                .ToList()
                .OrderBy(p => Guid.NewGuid()) // 隨機排序
                .Take(count)
                .Select(p => new
                {
                    p.ProductsId,
                    p.Name,
                    p.Price,
                    ImageUrl = p.ProductImages
                                .FirstOrDefault(img => img.IsMain == 1)?.ImageUrl ?? ""
                });

            return Ok(products);
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // 回傳 201 Created + 路由資訊
            return CreatedAtAction(nameof(GetProduct), new { id = product.ProductsId }, product);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.ProductsId)
            {
                return BadRequest();
            }

            _context.Entry(product).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ProductExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent(); // 204 無內容
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool ProductExists(int id)
        {
            return _context.Products.Any(e => e.ProductsId == id);
        }
    }
}
