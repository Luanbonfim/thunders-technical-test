namespace Thunders.TechTest.ApiService.Models.Dtos;

public class TollUsageDto
{
    public Guid? Id { get; set; }
    public DateTime UsageDateTime { get; set; }
    public string TollBooth { get; set; } 
    public string City { get; set; } 
    public string State { get; set; } 
    public decimal Amount { get; set; }
    public VehicleType VehicleType { get; set; }
} 