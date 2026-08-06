namespace Analyser.ForceSetProperties.Models
{
    public class SetLocation
    {
        public SetLocation(string fileName, int lineNumber, string? methodName = null)
        {
            FileName = fileName;
            LineNumber = lineNumber;
            MethodName = methodName;
        }

        public string FileName { get; }

        public int LineNumber { get; }

        public string? MethodName { get; }
    }
}
