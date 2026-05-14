namespace FlightKS.Models.Dtos.Bookings;

public class PaymentSummaryDto
{
    public decimal BaseFare { get; set; }
    public decimal TaxesFees { get; set; }
    public decimal TotalPrice { get; set; }
}
