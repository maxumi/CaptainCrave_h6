namespace Api.Models.Enums;

// Hvilken rolle en bruger har i systemet. Gemmes som tekst i databasen, så den
// er nem at læse direkte i databasen og ikke ændrer betydning, hvis rækkefølgen ændres.
public enum UserRole
{
    // En almindelig bruger, der kan bestille mad.
    Customer,

    // Ejer af en restaurant, der kan administrere menuer og ordrer.
    Restaurant,

    // Administrator med adgang til alt i systemet.
    Admin
}
