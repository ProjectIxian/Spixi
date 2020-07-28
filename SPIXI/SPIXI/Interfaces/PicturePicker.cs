using System.IO;
using System.Threading.Tasks;

namespace SPIXI.Interfaces
{
    public class SpixiImageData
    {
        public Stream stream = null;
        public string name = null;
        public string path = null;
    }

    public interface IPicturePicker
    {
        Task<SpixiImageData> PickImageAsync();
        byte[] ResizeImage(byte[] image_data, int width, int height, int quality);
    }
}
