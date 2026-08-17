using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

// Handles HTTP requests for category resources.
[ApiController]
[Route("api/[controller]")]
public class CategoriesController(ICategoryService categoryService) : ControllerBase
{
    private readonly ICategoryService _categoryService = categoryService;

    // Returns all categories belonging to the specified restaurant, across all of its menus.
    [HttpGet("restaurant/{restaurantId}")]
    public async Task<IActionResult> GetByRestaurant(int restaurantId)
    {
        var categories = await _categoryService.GetByRestaurantIdAsync(restaurantId);
        return Ok(categories);
    }

    // Returns all categories belonging to the specified menu.
    [HttpGet("menu/{menuId}")]
    public async Task<IActionResult> GetByMenu(int menuId)
    {
        var categories = await _categoryService.GetByMenuIdAsync(menuId);
        return Ok(categories);
    }

    // Creates a new category and returns it with a 201 status.
    [HttpPost]
    [Authorize(Roles = "Restaurant,Admin")]
    public async Task<IActionResult> Create(CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var created = await _categoryService.CreateAsync(dto);
        return Created(string.Empty, created);
    }
}
