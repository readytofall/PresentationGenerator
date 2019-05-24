using System.Runtime.Serialization;

namespace Presentation_Generator.Models
{
    [DataContract]
    public class Slide
    {
        [DataMember]
        public string PathToBackgroundPicture { get; set; }
        [DataMember]
        public string Title { get; set; }
        [DataMember]
        public string Text { get; set; }
        [DataMember]
        public string SlideHorizontalOffset { get; set; } = "0";
        [DataMember]
        public string SlideVerticalOffset { get; set; } = "100";
        [DataMember]
        public string TitleHorizontalOffset { get; set; } = "0";
        [DataMember]
        public string TitleVerticalOffset { get; set; } = "0";
    }
}