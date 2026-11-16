
namespace HumanitarianAidPlatform.Application.Shelters;

public enum ShelterOccupancyUpdateResult
{
    Updated,
    NotFound,
    InvalidChange,
    CapacityExceeded,
    WouldBecomeNegative
}
