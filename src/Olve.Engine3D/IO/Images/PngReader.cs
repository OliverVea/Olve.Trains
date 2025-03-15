using BigGustave;
using Olve.OpenRaster;
using Olve.Utilities.Types.Results;

namespace Olve.Engine3D.IO.Images;

public class PngFileReader : IImageFileReader<Png>
{
    public Result<Png> ReadImage(Stream stream)
    {
        return Png.Open(stream);
    }
}
