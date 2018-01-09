using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace SCDemoFaceRecWeb.Models
{
    [Serializable]
    public class Person
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
        [JsonProperty(PropertyName = "personId")]
        public string PersonId { get; set; }
        [JsonProperty(PropertyName = "fristCaptureDate")]
        public DateTime FirstCaptureDate { get; set; }
        [JsonProperty(PropertyName = "captures")]
        public List<Capture> Captures { get; set; }
    }

    [Serializable]
    public class Capture {
        [JsonProperty(PropertyName = "faceId")]
        public string FaceId { get; set; }
        [JsonProperty(PropertyName = "faceRectangle")]
        public Dictionary<string, string> FaceRectangle { get; set; } = new Dictionary<string, string>();
        [JsonProperty(PropertyName = "faceAttributes")]
        public Dictionary<string, string> FaceAttributes { get; set; } = new Dictionary<string, string>();
        [JsonProperty(PropertyName = "imageUrl")]
        public string ImageUrl { get; set; }
        [JsonProperty(PropertyName = "cameraId")]
        public string CameraId { get; set; }
        [JsonProperty(PropertyName = "captureDate")]
        public DateTime CaptureDate { get; set; }
        [JsonProperty(PropertyName = "numFaces")]
        public int NumberOfFaces { get; set; }
    }
}
