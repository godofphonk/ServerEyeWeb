namespace ServerEye.Core.DTOs.GoApi;

using System.Text.Json.Serialization;

public class GoApiStaticInfoResponse
{
    [JsonPropertyName("server_info")]
    public ServerInfo ServerInfo { get; set; } = new();

    [JsonPropertyName("hardware_info")]
    public HardwareInfo HardwareInfo { get; set; } = new();

    [JsonPropertyName("motherboard_info")]
    public MotherboardInfo MotherboardInfo { get; set; } = new();

    [JsonPropertyName("memory_modules")]
    public List<MemoryModule> MemoryModules { get; set; } = new();

    [JsonPropertyName("network_interfaces")]
    public List<NetworkInterfaceInfo> NetworkInterfaces { get; set; } = new();

    [JsonPropertyName("disk_info")]
    public List<DiskInfoDetail> DiskInfo { get; set; } = new();
}

public class ServerInfo
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string Hostname { get; set; } = string.Empty;

    [JsonPropertyName("os")]
    public string Os { get; set; } = string.Empty;

    [JsonPropertyName("os_version")]
    public string OsVersion { get; set; } = string.Empty;

    [JsonPropertyName("kernel")]
    public string Kernel { get; set; } = string.Empty;

    [JsonPropertyName("architecture")]
    public string Architecture { get; set; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class HardwareInfo
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("cpu_model")]
    public string CpuModel { get; set; } = string.Empty;

    [JsonPropertyName("cpu_cores")]
    public int CpuCores { get; set; }

    [JsonPropertyName("cpu_threads")]
    public int CpuThreads { get; set; }

    [JsonPropertyName("cpu_frequency_mhz")]
    public double CpuFrequencyMhz { get; set; }

    [JsonPropertyName("gpu_model")]
    public string GpuModel { get; set; } = string.Empty;

    [JsonPropertyName("gpu_driver")]
    public string GpuDriver { get; set; } = string.Empty;

    [JsonPropertyName("gpu_memory_gb")]
    public double GpuMemoryGb { get; set; }

    [JsonPropertyName("total_memory_gb")]
    public double TotalMemoryGb { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class MotherboardInfo
{
    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("bios_date")]
    public DateTime BiosDate { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class MemoryModule
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("slot_name")]
    public string SlotName { get; set; } = string.Empty;

    [JsonPropertyName("size_gb")]
    public double SizeGb { get; set; }

    [JsonPropertyName("memory_type")]
    public string MemoryType { get; set; } = string.Empty;

    [JsonPropertyName("frequency_mhz")]
    public double FrequencyMhz { get; set; }

    [JsonPropertyName("manufacturer")]
    public string Manufacturer { get; set; } = string.Empty;

    [JsonPropertyName("part_number")]
    public string PartNumber { get; set; } = string.Empty;

    [JsonPropertyName("speed_mts")]
    public int SpeedMts { get; set; }

    [JsonPropertyName("voltage")]
    public double Voltage { get; set; }

    [JsonPropertyName("timings")]
    public string Timings { get; set; } = string.Empty;

    [JsonPropertyName("ecc")]
    public bool Ecc { get; set; }

    [JsonPropertyName("registered")]
    public bool Registered { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class NetworkInterfaceInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("interface_name")]
    public string InterfaceName { get; set; } = string.Empty;

    [JsonPropertyName("mac_address")]
    public string MacAddress { get; set; } = string.Empty;

    [JsonPropertyName("interface_type")]
    public string InterfaceType { get; set; } = string.Empty;

    [JsonPropertyName("speed_mbps")]
    public long SpeedMbps { get; set; }

    [JsonPropertyName("vendor")]
    public string Vendor { get; set; } = string.Empty;

    [JsonPropertyName("driver")]
    public string Driver { get; set; } = string.Empty;

    [JsonPropertyName("is_physical")]
    public bool IsPhysical { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}

public class DiskInfoDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("server_id")]
    public string ServerId { get; set; } = string.Empty;

    [JsonPropertyName("device_name")]
    public string DeviceName { get; set; } = string.Empty;

    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("serial_number")]
    public string SerialNumber { get; set; } = string.Empty;

    [JsonPropertyName("size_gb")]
    public double SizeGb { get; set; }

    [JsonPropertyName("disk_type")]
    public string DiskType { get; set; } = string.Empty;

    [JsonPropertyName("interface_type")]
    public string InterfaceType { get; set; } = string.Empty;

    [JsonPropertyName("filesystem")]
    public string Filesystem { get; set; } = string.Empty;

    [JsonPropertyName("mount_point")]
    public string MountPoint { get; set; } = string.Empty;

    [JsonPropertyName("is_system_disk")]
    public bool IsSystemDisk { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
