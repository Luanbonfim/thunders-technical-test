namespace Thunders.TechTest.ApiService.Models;

public class TollUsage
{
    public Guid Id { get; set; }
    public DateTime UsageDateTime { get; set; }
    public string TollBooth { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public VehicleType VehicleType { get; set; }
}

public enum VehicleType
{
    Motorcycle,
    Car,
    Truck
} 