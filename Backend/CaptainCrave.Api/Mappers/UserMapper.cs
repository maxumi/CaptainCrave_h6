using Api.DTOs.Auth;
using Api.Models;

namespace Api.Mappers;

// Converts between User entities and auth DTOs
public static class UserMapper
{
    // Maps a User and a JWT token to the response sent back to the client
    public static AuthResponseDto ToAuthResponseDto(this User user, string token) =>
        new()
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Address = user.Address,
            Latitude = user.Latitude,
            Longitude = user.Longitude,
            Role = user.Role.ToString(),
            Token = token
        };

    // Maps a register request to a new User object (password is hashed separately in AuthService)
    public static User ToUser(this RegisterRequestDto dto) =>
        new()
        {
            Name = dto.Name,
            Email = dto.Email,
            Address = dto.Address,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            Role = dto.Role
        };
}
