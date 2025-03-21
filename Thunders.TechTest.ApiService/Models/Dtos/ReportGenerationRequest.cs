using System.ComponentModel.DataAnnotations;
using Thunders.TechTest.ApiService.Messages;

namespace Thunders.TechTest.ApiService.Models.Dtos;

public class ReportGenerationRequest
{
    public DateTime StartDate { get; set; }

    public DateTime EndDate { get; set; }

    public Dictionary<string, object> Parameters { get; set; } = new();

    public ReportType ReportType { get; set; }
} 