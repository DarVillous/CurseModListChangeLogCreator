using ICSharpCode.SharpZipLib.Zip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace CurseModListChangelogCreator2
{
    public class StringStaticDataSource : IStaticDataSource, IDisposable
    {
        private MemoryStream  _contents;

        public StringStaticDataSource(string contents)
        {
            _contents = new MemoryStream();
            using (StreamWriter writer = new StreamWriter(_contents, Encoding.Default, 1024, true))
            {
                writer.Write(contents);
            }
        }

        public void Dispose()
        {
            if(_contents != null)
            {
                _contents.Dispose();
            }
        }

        public Stream GetSource()
        {

            _contents.Position = 0;
            return _contents;
        }
    }
}
