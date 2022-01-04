using System.IO;
using System.Xml.Serialization;

namespace Fujitsu.eDoc.BOMApplicationDesktopApp
{
    public static class Serialization
    {
        public static string Serialize<T>(this T toSerialize)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
            StringWriter textWriter = new StringWriter();
            xmlSerializer.Serialize(textWriter, toSerialize);
            return textWriter.ToString();
        }
    }
}
