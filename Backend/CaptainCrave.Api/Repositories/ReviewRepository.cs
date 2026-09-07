using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Repositories;

// Denne klasse snakker direkte med databasen (via EF Core) og henter/gemmer anmeldelser.
public class ReviewRepository : IReviewRepository
{
    private readonly AppDbContext _context;

    // Opretter repository-klassen med den databaseforbindelse, som alle opslåg bruger.
    public ReviewRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>Regner en restaurants gennemsnitlige stjernevurdering og antal anmeldelser ud i ét samlet opslag.</summary>

    /// <returns>Gennemsnittet (rundet til 2 dpr.) og antallet af anmeldelser. Begge er 0, hvis der ingen anmeldelser er.</returns>
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

    /// <summary>
    /// Regner vurderinger ud for flere restauranter på én gang, så vi slår databasen op én
    /// eneste gang i stedet for at spørge en gang pr. restaurant (meget hurtigere).
    /// </summary>
    /// <returns>En ordbog fra restaurant-id til (gennemsnit, antal anmeldelser).</returns>
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

    /// <summary>Henter én anmeldelse ud fra id.</summary>
    /// <returns>Anmeldelsen, eller null hvis den ikke findes.</returns>
    public async Task<Review?> GetByIdAsync(int id)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <summary>Finder den anmeldelse, en bestemt bruger har skrevet om en bestemt restaurant.</summary>
    /// <returns>Brugerens anmeldelse af restauranten, eller null hvis der ikke er skrevet en.</returns>
    public async Task<Review?> GetByUserAndRestaurantAsync(int userId, int restaurantId)
    {
        return await _context.Reviews
            .FirstOrDefaultAsync(r => r.UserId == userId && r.RestaurantId == restaurantId);
    }

    /// <summary>Gemmer en helt ny anmeldelse i databasen.</summary>
    /// <returns>Den gemte anmeldelse, nu med et rigtigt Id.</returns>
    public async Task<Review> CreateAsync(Review review)
    {
        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return review;
    }

    /// <summary>Gemmer ændringer på en anmeldelse, der allerede findes i databasen.</summary>
    /// <returns>Den opdaterede anmeldelse.</returns>
    public async Task<Review> UpdateAsync(Review review)
    {
        _context.Reviews.Update(review);
        await _context.SaveChangesAsync();

        return review;
    }
}
