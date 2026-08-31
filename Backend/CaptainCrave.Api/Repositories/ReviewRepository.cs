using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;

    public ReviewRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(double AverageRating, int ReviewCount)> GetSummaryAsync(int restaurantId)
    {
        var summary = await _context.Reviews
            .Where(r => r.RestaurantId == restaurantId)
            .GroupBy(r => 1)
            .Select(g => new { Count = g.Count(), Average = g.Average(r => r.Rating) })
            .FirstOrDefaultAsync();

        return summary is null
            ? (0, 0)
            : (Math.Round(summary.Average, 2), summary.Count);
    }

    public async Task<Dictionary<int, (double AverageRating, int ReviewCount)>> GetSummariesByRestaurantIdsAsync(
        IEnumerable<int> restaurantIds)
    {
        var ids = restaurantIds.ToList();
        if (ids.Count == 0)
        {
            return [];
        }

        var summaries = await _context.Reviews
            .Where(r => ids.Contains(r.RestaurantId))
            .GroupBy(r => r.RestaurantId)
            .Select(g => new { RestaurantId = g.Key, Count = g.Count(), Average = g.Average(r => r.Rating) })
            .ToListAsync();

        return summaries.ToDictionary(
            s => s.RestaurantId,
            s => (Math.Round(s.Average, 2), s.Count));
    }

    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<Review?> GetByUserAndRestaurantAsync(int userId, int restaurantId)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.RestaurantId == restaurantId);
    }

    public async Task<Review> CreateAsync(Review review)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return review;
    }

    public async Task<Review> UpdateAsync(Review review)
    {
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();

        return review;
    }
}