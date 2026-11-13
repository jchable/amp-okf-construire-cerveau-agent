using Acme.Orders;

// La date de clôture est fixe pour rejouer les mêmes commandes en répétition.
var reportingDate = new DateOnly(2026, 11, 12);
Console.WriteLine($"Acme Retail — chiffre d’affaires au {reportingDate:dd/MM/yyyy}");

foreach (var daysSinceDelivery in new[] { 1, 31 })
{
    var deliveredOn = reportingDate.AddDays(-daysSinceDelivery);
    var recognized = RevenuePolicy.IsRevenueRecognized(deliveredOn, reportingDate);
    var verdict = recognized ? "comptabilisée" : "en attente";
    Console.WriteLine($"Commande livrée depuis {daysSinceDelivery} j ({deliveredOn:dd/MM/yyyy}) — {verdict}");
}
