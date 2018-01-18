var request = require('request'); //https://github.com/request/request
var rp = require('request-promise');

// Global constants
var uri_base = "https://westus2.api.cognitive.microsoft.com/face/v1.0/";
var face_api_key = "6118b8b9a4c44754908be910ce58a18e"; 
var face_list_id = "01";
var face_recog_thresh = 0.5;
    
module.exports = function (context, eventGridEvent, outputDocument) {
  if (eventGridEvent.eventType != 'Microsoft.Storage.BlobCreated' || eventGridEvent.data.contentType != 'image/jpeg') {
    context.log('Incorrect format of EventGrid Event received: exiting.');
    context.res = {
      status: 400,
      body: "Please check the image got uploaded to Blob Storage correctly."
    };
    context.done();
  };

  //add sas token authentication to blob image url
  var image_url = eventGridEvent.data.url;
  var image_url_sas = eventGridEvent.data.url + process.env['SasToken'];

  // Request parameters.
  var params = {
    "returnFaceId": "true",
    "returnFaceLandmarks": "false",
    "returnFaceAttributes": "age,gender,headPose,smile,facialHair,glasses,emotion,hair,makeup,occlusion,accessories,blur,exposure,noise",
  };

  // Request for detecting faces in the image
  var options = {
    method: 'POST',
    uri: uri_base + "detect?" + formatParams(params),
    headers: {
      'Content-Type': 'application/json',
      'Ocp-Apim-Subscription-Key': face_api_key
    },
    body: {
      "url": image_url_sas
    },
    json: true
  };

  var num_faces = -1;
  var first_face = true;
  var face_data = {};
  context.log('Detecting faces in the image...');

  rp(options)
    .then(function (response) {
      num_faces = response.length;
      // Break when there are no faces recognized in the picture
      if (num_faces <= 0){
        throw new Error("No face found in the image: exiting...");                
      }
      // Only analyzing the largest face in the image
      face_data = response[0];
      return(checkFaceList(face_data));

    }).then(function(detected_list) {
      if (detected_list.length == 0 || detected_list[0].confidence < face_recog_thresh) { //confidence level is not high enough to determine face is in list
        context.log("No similar faces found in face list (confidence level in facial recognition < 50%): adding face to persisted face list.");
        // add face to the persisted face list
        return(addToFaceList(image_url_sas));

      } else { 
        // face is detected in the persisted face list
        var sim_face = detected_list[0];
        context.log("A similar face with confidence level " + sim_face.confidence + " has been found in the persisted face list.");
        first_face = false;
        return(sim_face);
      }

    }).then(function(response) {
      var camera_id = getCameraId(image_url);
      var confidence = -1;
      if (response.confidence) confidence = response.confidence;
      writeNewDocument(context, response.persistedFaceId, face_data, image_url, camera_id, num_faces, first_face, confidence);
    
    }).catch(function (response) {
      // One of the chained reuests failed...
      context.log('Face Recognition failed: '+ response); 

    }).finally(function(){
      context.log('Finishing up...');
      context.done();
    });
};

function formatParams(obj) {
  return Object.keys(obj).map((k) => encodeURIComponent(k) + '=' + encodeURIComponent(obj[k])).join('&');
};

function getCameraId(image_url) {
  // Sample image URLS: https://scdemophotorepo01.blob.core.windows.net/images/yyz01_20180108120700.jpg OR
  // https://scdemophotorepo01.blob.core.windows.net/images/YYCL02/YYCL0220180109212647.jpg
  var start_index = image_url.lastIndexOf('/');
  var end_index = image_url.lastIndexOf('_');
  if (end_index < 0) { // in order to work for Linux RPi picture format
      end_index = start_index + 7;
  }
  return image_url.substring(start_index + 1, end_index);
};

function checkFaceList(face_data) {
  // Check whether the face id is similar to the list of persisted face ids
  options = {
    method: 'POST',
    uri: uri_base + "findsimilars/",
    headers: {
      'Content-Type': 'application/json',
      'Ocp-Apim-Subscription-Key': face_api_key
    },
    body: {
      "faceId": face_data.faceId,
      "faceListId": face_list_id,
      "maxNumOfCandidatesReturned": 3
    },
    json: true
  };
  return rp(options);
};

function addToFaceList(image_url_sas) {
  // Add face id to list of persisted face ids
  options = {
    method: 'POST',
    uri: uri_base + "facelists/" + face_list_id + "/persistedFaces?faceListId=" + face_list_id,
    headers: {
      'Content-Type': 'application/json',
      'Ocp-Apim-Subscription-Key': face_api_key
    },
    body: {
      "url": image_url_sas
    },
    json: true
  };
  return rp(options);
};

function writeNewDocument(context, persistedFaceId, face_data, image_url, camera_id, num_faces, first_face, confidence) {
  // Write a document to CosmosDB for the face that has been found in the picture 
  context.bindings.faceDocument = JSON.stringify({
    persistedFaceId: persistedFaceId,
    faceRectangle: face_data.faceRectangle,
    faceAttributes: face_data.faceAttributes,
    imageUrl: image_url,
    cameraID: camera_id,
    numFaces: num_faces,
    first_face: first_face,
    confidence: confidence
  });
  context.log("Writing face with PersistedFaceId: " + persistedFaceId + " to CosmosDB.");
};