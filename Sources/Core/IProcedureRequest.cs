namespace BlueBlaze.Core;

public interface IProcedureRequest
{
    string Nsid { get; }

    ILexiconInput? Input { get; }
}
