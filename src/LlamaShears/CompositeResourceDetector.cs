using OpenTelemetry.Resources;

namespace LlamaShears;

public sealed class CompositeResourceDetector : IResourceDetector
{
    private readonly IEnumerable<IResourceDetector> _detectors;

    public CompositeResourceDetector(IEnumerable<IResourceDetector> detectors)
    {
        _detectors = detectors;
    }

    public Resource Detect()
    {
        var resource = Resource.Empty;
        foreach (var detector in _detectors)
        {
            resource = resource.Merge(detector.Detect());
        }
        return resource;
    }
}
