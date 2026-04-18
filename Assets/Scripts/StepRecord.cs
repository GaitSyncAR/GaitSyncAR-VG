[System.Serializable]
public class StepRecord
{
    public bool isRightFoot;
    public long timeStampUs; // Microseconds from hardware

    // constructor
    public StepRecord(bool isRightFoot, long timeStampUs)
    {
        this.isRightFoot = isRightFoot;
        this.timeStampUs = timeStampUs;
    }
}