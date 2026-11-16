namespace TKSC.Application.Services;
// Section "GoodsOrders" in configuration. An unpaid goods order holds its stock for HoldMinutes (RP3 UC-36, JOB-04).
public sealed class GoodsOrderOptions
{
    public int HoldMinutes { get; set; } = 15;
    public int ExpirySweepSeconds { get; set; } = 30;
    public bool ExpiryWorkerEnabled { get; set; } = true;
}
