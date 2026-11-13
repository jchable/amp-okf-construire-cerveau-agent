namespace Acme.Orders;

/// <summary>Applique la politique Acme de reconnaissance du chiffre d’affaires.</summary>
public static class RevenuePolicy
{
    /// <summary>
    /// Une commande livrée entre dans le chiffre d’affaires à la clôture
    /// du délai de retour, 30 jours après sa date de livraison.
    /// Une commande sans date de livraison reste en attente.
    /// </summary>
    public static bool IsRevenueRecognized(DateOnly? deliveredOn, DateOnly reportingDate)
        => deliveredOn is DateOnly delivery && reportingDate.DayNumber - delivery.DayNumber >= 30;
}
