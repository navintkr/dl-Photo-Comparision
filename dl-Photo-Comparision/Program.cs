using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.CognitiveServices.Vision.Face;
using Microsoft.Azure.CognitiveServices.Vision.Face.Models;

namespace FaceQuickstart
{
    class Program
    {
        static string personGroupId = Guid.NewGuid().ToString();

        // URL path for the images.
        const string IMAGE_BASE_URL = "https://raw.githubusercontent.com/Azure-Samples/cognitive-services-sample-data-files/master/Face/images/";
        const string imageOnFileUrl = "https://navkrishtempstorage.blob.core.windows.net/test/Naveen-DL.jpeg";
        const string imageToVerify = "https://navkrishtempstorage.blob.core.windows.net/test/Untitled.png";
        // From your Face subscription in the Azure portal, get your subscription key and endpoint.
        const string SUBSCRIPTION_KEY = "b9d6aec825cb427abe8e67a3c053b07f";
        const string ENDPOINT = "https://navkrish-facerecog.cognitiveservices.azure.com/";


        static void Main(string[] args)
        {
            // Recognition model 4 was released in 2021 February.
            // It is recommended since its accuracy is improved
            // on faces wearing masks compared with model 3,
            // and its overall accuracy is improved compared
            // with models 1 and 2.
            const string RECOGNITION_MODEL4 = RecognitionModel.Recognition04;

            // Authenticate.
            IFaceClient client = Authenticate(ENDPOINT, SUBSCRIPTION_KEY);

            // Identify - recognize a face(s) in a person group (a person group is created in this example).
            Verify(client, imageOnFileUrl, imageToVerify, RECOGNITION_MODEL4).Wait();

            Console.WriteLine("End of quickstart.");
        }

        /*
         *	AUTHENTICATE
         *	Uses subscription key and region to create a client.
         */
        public static IFaceClient Authenticate(string endpoint, string key)
        {
            return new FaceClient(new ApiKeyServiceClientCredentials(key)) { Endpoint = endpoint };
        }

        // Detect faces from image url for recognition purposes. This is a helper method for other functions in this quickstart.
        // Parameter `returnFaceId` of `DetectWithUrlAsync` must be set to `true` (by default) for recognition purposes.
        // Parameter `FaceAttributes` is set to include the QualityForRecognition attribute. 
        // Recognition model must be set to recognition_03 or recognition_04 as a result.
        // Result faces with insufficient quality for recognition are filtered out. 
        // The field `faceId` in returned `DetectedFace`s will be used in Face - Face - Verify and Face - Identify.
        // It will expire 24 hours after the detection call.
        private static async Task<List<DetectedFace>> DetectFaceRecognize(IFaceClient faceClient, string url, string recognition_model)
        {
            // Detect faces from image URL. Since only recognizing, use the recognition model 1.
            // We use detection model 3 because we are not retrieving attributes.
            IList<DetectedFace> detectedFaces = await faceClient.Face.DetectWithUrlAsync(url, recognitionModel: recognition_model, detectionModel: DetectionModel.Detection03, returnFaceAttributes: new List<FaceAttributeType> { FaceAttributeType.QualityForRecognition });
            List<DetectedFace> sufficientQualityFaces = new List<DetectedFace>();
            foreach (DetectedFace detectedFace in detectedFaces)
            {
                var faceQualityForRecognition = detectedFace.FaceAttributes.QualityForRecognition;
                if (faceQualityForRecognition.HasValue && (faceQualityForRecognition.Value >= QualityForRecognition.Medium))
                {
                    sufficientQualityFaces.Add(detectedFace);
                }
            }
            Console.WriteLine($"{detectedFaces.Count} face(s) with {sufficientQualityFaces.Count} having sufficient quality for recognition detected from image `{Path.GetFileName(url)}`");

            return sufficientQualityFaces.ToList();
        }

        public static async Task Verify(IFaceClient client, string imageOnFileUrl, string imageToVerify, string recognitionModel03)
        {
            Console.WriteLine("========VERIFY========");
            Console.WriteLine();

            string targetImageFileNames = imageOnFileUrl;
            string sourceImageFileName1 = imageToVerify;

            List<Guid> targetFaceIds = new List<Guid>();
            
                // Detect faces from target image url.
            List<DetectedFace> detectedFaces = await DetectFaceRecognize(client, $"{imageOnFileUrl} ", recognitionModel03);
            targetFaceIds.Add(detectedFaces[0].FaceId.Value);
            Console.WriteLine($"{detectedFaces.Count} faces detected from image `{imageOnFileUrl}`.");
            

            // Detect faces from source image file 1.
            List<DetectedFace> detectedFaces1 = await DetectFaceRecognize(client, $"{imageToVerify} ", recognitionModel03);
            Console.WriteLine($"{detectedFaces1.Count} faces detected from image `{imageToVerify}`.");
            Guid sourceFaceId1 = detectedFaces1[0].FaceId.Value;


            // Verification example for faces of the same person.
            VerifyResult verifyResult1 = await client.Face.VerifyFaceToFaceAsync(sourceFaceId1, targetFaceIds[0]);
            Console.WriteLine(
                verifyResult1.IsIdentical
                    ? $"Faces from {sourceImageFileName1} & {targetImageFileNames[0]} are of the same (Positive) person, similarity confidence: {verifyResult1.Confidence}."
                    : $"Faces from {sourceImageFileName1} & {targetImageFileNames[0]} are of different (Negative) persons, similarity confidence: {verifyResult1.Confidence}.");

            Console.WriteLine();
        }
    }
}