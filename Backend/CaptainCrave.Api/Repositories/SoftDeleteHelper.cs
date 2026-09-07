using Api.Models;

namespace Api.Repositories;

// Nogle fælles små hjælpe-metoder til soft delete, så hvert repository ikke skal skrive
// de samme fire linjer kode igen og igen for at markere noget som slettet/gendannet.
public static class SoftDeleteHelper
{
    // Markerer en entitet som slettet (IsDeleted = true) og gemmer tidspunktet for både
    // sletningen og den generelle UpdatedAt-opdatering.
    public static void MarkDeleted<T>(T entity) where T : ISoftDeletable, IAuditable
    {
        // <T> gør metoden genbrugelig. Kravene efter "where" sikrer, at typen
        // har både soft-delete-felter og auditfelter. Det undgår gentaget kode.
        var now = DateTime.UtcNow;
        entity.IsDeleted = true;
        entity.DeletedAt = now;
        entity.UpdatedAt = now;
    }

    // Fjerner slette-markeringen fra en tidligere slettet entitet (IsDeleted = false),
    // nulstiller DeletedAt, og opdaterer UpdatedAt til nu.
    public static void MarkRestored<T>(T entity) where T : ISoftDeletable, IAuditable
    {
        // Restore fjerner slettemarkeringen; den oprindelige række og dens id bevares.
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        entity.UpdatedAt = DateTime.UtcNow;
    }
}
