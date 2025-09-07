using System.IO;
using TagLib;
namespace Floaty_Music.Utils
{


    public class StreamFileAbstraction : TagLib.File.IFileAbstraction
    {
        public StreamFileAbstraction(string name, Stream stream)
        {
            Name = name;
            ReadStream = stream;
            WriteStream = stream;
        }

        public string Name { get; }

        public Stream ReadStream { get; }

        public Stream WriteStream { get; }

        public void CloseStream(Stream stream)
        {
            // don't dispose here, ASP.NET Core will manage the stream
        }
    }

}
