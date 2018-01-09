using System;
using System.Collections.Generic;

namespace SCDemoFaceRecWeb.Models
{
    [Serializable]
    public class Person
    {
        public string Id { get; set; }
        public string PersonId { get; set; }
        public DateTime FirstCaptureDate { get; set; }
        public List<Capture> Captures { get; set; }
    }

    [Serializable]
    public class Capture {
        public string FaceId { get; set; }
        public Dictionary<string, string> FaceRectangle { get; set; } = new Dictionary<string, string>();
        public Dictionary<string, string> FaceAttributes { get; set; } = new Dictionary<string, string>();
        public string ImageUrl { get; set; }
        public string CameraId { get; set; }
        public DateTime CaptureDate { get; set; }
    }
}
