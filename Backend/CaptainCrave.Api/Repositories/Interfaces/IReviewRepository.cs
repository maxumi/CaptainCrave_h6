using Api.Models;

namespace Api.Repositories;

public interface IReviewRepository
{
    Task<List<Review>> GetByRestaurantIdAsync(int restaurantId);

    Task<Review?> GetByIdAsync(int id);

    Task<Review?> GetByUserAndRestaurantAsync(int userId, int restaurantId);

    Task<Review> CreateAsync(Review review);

    Task<Review> UpdateAsync(Review review);
}