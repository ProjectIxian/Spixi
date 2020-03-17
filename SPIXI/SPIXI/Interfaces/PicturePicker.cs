using System.IO;
using System.Threading.Tasks;

namespace SPIXI.Interfaces
{
    public interface IPicturePicker
    {
        Task<Stream> GetImageStreamAsync();
        byte[] ResizeImage(byte[] image_data, int width, int height);
    }
}
