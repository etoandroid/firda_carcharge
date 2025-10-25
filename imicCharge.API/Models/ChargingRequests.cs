namespace imicCharge.API.Models
{
    public class StartChargingRequest
    {
        public required string ChargerId { get; set; }
    }

    public class StopChargingRequest
    {
        public required string ChargerId { get; set; }
    }
}