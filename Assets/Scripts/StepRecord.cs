[System.Serializable]
public class StepRecord
{
    public bool isRightFoot;
    public long timestampUs; // Microseconds from hardware

    // constructor
    public StepRecord(bool isRightFoot, long timestampUs)
    {
        this.isRightFoot = isRightFoot;
        this.timestampUs = timestampUs;
    }
}