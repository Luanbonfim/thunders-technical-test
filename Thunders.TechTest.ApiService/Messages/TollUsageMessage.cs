using Thunders.TechTest.ApiService.Models;
using Thunders.TechTest.ApiService.Models.Dtos;

namespace Thunders.TechTest.ApiService.Messages;

public class TollUsageMessage
{
    public Guid Id { get; set; }
    public List<TollUsageDto> TollUsages { get; set; }
}

