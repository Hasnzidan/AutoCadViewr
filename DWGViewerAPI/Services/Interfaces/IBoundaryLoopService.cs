namespace DWGViewerAPI.Services.Interfaces
{
    public interface IBoundaryLoopService
    {
        List<double[]> ProcessEdges(IEnumerable<object> edges, string entityType);
        List<double[]> FinalizeLoop(List<double[]> points);
    }
}