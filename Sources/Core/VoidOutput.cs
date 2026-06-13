namespace BlueBlaze.Core;

public sealed class VoidOutput
{
    private VoidOutput()
    {
    }

    public static readonly VoidOutput Instance = new();
}
