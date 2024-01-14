using Newtonsoft.Json;

namespace SmartCarWebApp.Models;

public class Vehicle
{
    public VehicleInfo VehicleInfo { get; set; } = new VehicleInfo();

    public LockStatus Security { get; set; } = new LockStatus();

    //public bool IsLocked { get; set; } = default!;
    
    //public string FrontLeftWindowStatus { get; set; } = string.Empty;
    
    //public string FrontLeftDoorStatus { get; set; } = string.Empty;

    public string FrontStorageStatus { get; set; } = string.Empty;
    
    public string RearStorageStatus { get; set; } = string.Empty;

    public string SunroofStatus { get; set; } = string.Empty;

    public string ChargingPortStatus { get; set; } = string.Empty;

    public void SetStatus(LockStatus lockStatus)
    {
        //this.IsLocked = lockStatus.IsLocked;

        //this.FrontLeftWindowStatus = GetStatus(lockStatus.Windows, "frontLeft");
        //this.FrontLeftDoorStatus = GetStatus(lockStatus.Doors, "frontLeft");
        this.SunroofStatus = GetStatus(lockStatus.Sunroof, "sunroof");
        if (this.SunroofStatus == this.NotFoundValue)
        {
            // try a different value
        this.SunroofStatus = GetStatus(lockStatus.Sunroof, "front");
        }
        this.ChargingPortStatus = GetStatus(lockStatus.ChargingPort, "chargingPort");
        this.FrontStorageStatus = GetStatus(lockStatus.Storage, "front");
        this.RearStorageStatus = GetStatus(lockStatus.Storage, "rear");
    }

    private string GetStatus(List<TypeAndStatus> lockStatusList, string find)
    {
        TypeAndStatus? status = lockStatusList.FirstOrDefault(status => status.Type == find);
        return (status == null) ? this.NotFoundValue : status.Status;
    }

    private string NotFoundValue = "Not Found";
}

public class VehicleInfo
{
    [JsonProperty("id")]
    public string ID { get; set; } = default!;

    [JsonProperty("make")]
    public string Make { get; set; } = default!;

    [JsonProperty("model")]
    public string Model { get; set; } = default!;

    [JsonProperty("year")]
    public string Year { get; set; } = default!;
}

public class VehicleList
{
    [JsonProperty("vehicles")]
    public List<string> VehicleIDs { get; set; } = new List<string>();

    [JsonProperty("paging")]
    public Paging Paging { get; set; } = default!;
}

public class Paging
{
    [JsonProperty("count")]
    public string Count { get; set; } = default!;

    [JsonProperty("offset")]
    public string Offset { get; set; } = default!;
}

public class TypeAndStatus
{
    [JsonProperty("type")]
    public string Type { get; set; } = default!;

    [JsonProperty("status")]
    public string Status { get; set; } = default!;
}

public class LockStatus
{
    [JsonProperty("isLocked")]
    public bool IsLocked { get; set; } = default!;

    [JsonProperty("doors")]
    public List<TypeAndStatus> Doors { get; set; } = new List<TypeAndStatus>();

    [JsonProperty("windows")]
    public List<TypeAndStatus> Windows { get; set; } = new List<TypeAndStatus>();

    [JsonProperty("sunroof")]
    public List<TypeAndStatus> Sunroof { get; set; } = new List<TypeAndStatus>();

    [JsonProperty("storage")]
    public List<TypeAndStatus> Storage { get; set; } = new List<TypeAndStatus>();

    [JsonProperty("chargingPort")]
    public List<TypeAndStatus> ChargingPort { get; set; } = new List<TypeAndStatus>();
}
