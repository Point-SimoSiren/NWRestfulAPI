using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NWRestfulAPI.Models;

namespace NWRestfulAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly NorthwindOriginalContext _context = new NorthwindOriginalContext();



        // GET: api/Products
        [HttpGet]
        public ActionResult GetProducts()
        {
            try
            {
              var products = _context.Products.Join(
                    _context.Categories,
                    p => p.CategoryId,
                    c => c.CategoryId,
                    (p, c) => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.SupplierId,
                        p.CategoryId,
                        CategoryName = c.CategoryName,
                        p.QuantityPerUnit,
                        p.UnitPrice,
                        p.UnitsInStock,
                        p.UnitsOnOrder,
                        p.ReorderLevel,
                        p.Discontinued
                    }
                ).ToList();
                if (products.Count == 0)
                {
                    return NoContent();
                }
                else
                {
                    return Ok(products);
                }


            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
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



        [HttpGet("/catname/{catname}")]
        public async Task<ActionResult<Product>> GetProductsByName(string catname)
        {
            var products = _context.Products.Where(p => p.Category.CategoryName.StartsWith(catname)).Join(
                    _context.Categories,
                    p => p.CategoryId,
                    c => c.CategoryId,
                    (p, c) => new
                    {
                        p.ProductId,
                        p.ProductName,
                        p.SupplierId,
                        p.CategoryId,
                        CategoryName = c.CategoryName,
                        p.QuantityPerUnit,
                        p.UnitPrice,
                        p.UnitsInStock,
                        p.UnitsOnOrder,
                        p.ReorderLevel,
                        p.Discontinued
                    }
                ).ToList();

            Console.WriteLine(products);

            if (products == null)
            {
                return NoContent();
            }
            else
            {
                return Ok(products);
            }
        }






        // PUT: api/Products/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutProduct(int id, Product product)
        {
            if (id != product.ProductId)
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

            return NoContent();
        }

        // POST: api/Products
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Product>> PostProduct(Product product)
        {
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetProduct", new { id = product.ProductId }, product);
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
            return _context.Products.Any(e => e.ProductId == id);
        }
    }
}
