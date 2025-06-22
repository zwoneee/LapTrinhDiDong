using ECommerceSystem.Shared.DTOs.Product;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/admin/orders")]
[ApiController]
//[Authorize(Roles = "Admin")]
public class AdminOrdersController : ControllerBase
{
    private readonly WebDBContext _context;

    public AdminOrdersController(WebDBContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _context.Orders
            .Include(o => o.OrderItems)
            .ToListAsync();

        var result = orders.Select(o => new OrderDTO
        {
            Id = o.Id,
            UserId = o.UserId,
            Total = o.Total,
            Status = o.Status,
            DeliveryLocation = o.DeliveryLocation,
            Items = o.OrderItems.Select(i => new OrderItemDTO
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        });

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null) return NotFound();

        return Ok(new OrderDTO
        {
            Id = order.Id,
            UserId = order.UserId,
            Total = order.Total,
            Status = order.Status,
            DeliveryLocation = order.DeliveryLocation,
            Items = order.OrderItems.Select(i => new OrderItemDTO
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                Price = i.Price
            }).ToList()
        });
    }

    [HttpPost("{id}/update-status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        await _context.SaveChangesAsync();

        return Ok();
    }
}
