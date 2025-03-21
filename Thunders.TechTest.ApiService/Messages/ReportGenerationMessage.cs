namespace Thunders.TechTest.ApiService.Messages
{
    public class ReportGenerationMessage
    {
        public DateTime GeneratedAt { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public ReportType ReportType { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    public enum ReportType
    {
        HourlyByCityReport,
        TopTollboothsReport,
        VehicleTypesByTollboothReport
    }
}
