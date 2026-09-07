using Api.DTOs.Auth;
using Api.Models;

namespace Api.Mappers;

// Denne klasse hjælper os med at bygge om mellem User-modeller og de DTO'er, vi bruger til login/registrering.
public static class UserMapper
{
    /// <summary>
    /// Bygger det svar, klienten får lige efter login eller registrering: brugerens egne
    /// oplysninger sammen med et JWT-token, som appen bruger til at bevise, hvem man er, i alle senere kald.
    /// </summary>
    /// <returns>En AuthResponseDto med brugerdata og token, klar til at blive sendt til klienten.</returns>
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

    /// <summary>
    /// Bygger en ny bruger ud fra det, nogen har udfyldt i registreringsformularen.
    /// Selve kodeordet bliver IKKE gemt her, det bliver hashet (krypteret) et andet
    /// sted i AuthService, så vi aldrig gemmer et rigtigt kodeord i klartekst.
    /// </summary>
    /// <returns>En ny User, klar til at blive gemt (uden kodeord endnu).</returns>
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
