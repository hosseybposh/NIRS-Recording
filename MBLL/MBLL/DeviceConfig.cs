using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Xml.Serialization;
using System.IO;

namespace MBLL
{
    [Serializable()]
    public class DeviceConfig
    {
        public int FirstWaveLength { get; set; }

        public int SecondWaveLength { get; set; }

        public double Interval { get; set; }

        public int ChannelCount { get; set; }

        public List<ChannelSetting> ChannelsSetting { get; set; }

        public static void Serialize(DeviceConfig tData,string filename)
        {
            var serializer = new XmlSerializer(typeof(DeviceConfig));

            TextWriter writer = new StringWriter();
            serializer.Serialize(writer, tData);

             System.IO.File.WriteAllText(filename, writer.ToString());
        }

        public static DeviceConfig Deserialize(string filename)
        {
            var serializer = new XmlSerializer(typeof(DeviceConfig));

            TextReader reader = new StringReader(System.IO.File.ReadAllText( filename));

            return (DeviceConfig)serializer.Deserialize(reader);
        }

        public float Epsilon_Hb_735 { get; set; }

        public float Epsilon_Hbo2_735 { get; set; }

        public float Epsilon_Hb_850 { get; set; }

        public float Epsilon_Hbo2_850 { get; set; }
    }

    public struct ChannelSetting
    {

        Color clrGrid;
        [XmlIgnore]
        public Color GraphColor
        {
            get { return clrGrid; }
            set { clrGrid = value; }
        }
        [XmlElement("ClrGrid")]
        public string GraphColorHtml
        {
            get { return ColorTranslator.ToHtml(clrGrid); }
            set { GraphColor = ColorTranslator.FromHtml(value); }
        }

        public bool Visible { get; set; }

        public float SensorDetectorDistance { get; set; }

        public string ChannelName { get; set; }
    }

}
